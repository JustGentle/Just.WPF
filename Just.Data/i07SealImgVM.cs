using Dapper;
using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Crypto;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Just.Data
{
    [AddINotifyPropertyChangedInterface]
    public class i07SealImgVM
    {
        public string ConnectionString07 { get; set; } = "Data Source =.; Initial Catalog = iOffice; User ID = sa; Password = asdASD!@#;";
        public string ConnectionString10 { get; set; } = "Data Source =.; Initial Catalog = iOffice10; User ID = sa; Password = asdASD!@#;";
        public string SealNames { get; set; } = string.Empty;
        public bool IsOverwrite { get; set; } = false;
        public bool IsStopWhenError { get; set; } = true;
        public string LogContent { get; set; }
        public int PageSize { get; set; } = 100;
        public bool Doing { get; set; }

        public string AttachmentDBName { get; set; } = "iOffice10.Attachment";
        public string PrivilegeDBName { get; set; } = "iOffice10.Privilege";

        #region Action
        private CancellationTokenSource cts;
        private CancellationToken token;
        private ICommand _Do;
        public ICommand Do
        {
            get
            {
                _Do = _Do ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (!Doing)
                    {
                        if (string.IsNullOrWhiteSpace(ConnectionString07))
                        {
                            MainWindowVM.NotifyWarn("请先配置07链接字符串");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(ConnectionString10))
                        {
                            MainWindowVM.NotifyWarn("请先配置10链接字符串");
                            return;
                        }
                        Exec();
                    }
                    else
                    {
                        Stop();
                    }
                });
                return _Do;
            }
        }
        private void Exec()
        {
            Doing = true;
            var start = DateTime.Now;
            AddLog($"{DateTime.Now:HH:mm:ss.fff} >>开始执行");
            cts = new CancellationTokenSource();
            token = cts.Token;
            var task = Task.Run(() =>
            {
                try
                {
                    //显示时间
                    var t = Task.Run(() =>
                    {
                        while (!token.IsCancellationRequested && Doing)
                        {
                            MainWindowVM.ShowStatus($"{DateTime.Now - start}");
                            Thread.Sleep(1000);
                        }
                    }, token);

                    using (IDbConnection connection09 = new System.Data.SqlClient.SqlConnection(ConnectionString07))
                    {
                        using (IDbConnection connection10 = new System.Data.SqlClient.SqlConnection(ConnectionString10))
                        {
                            AddLog($"{DateTime.Now:HH:mm:ss.fff} 查询10附件存储配置");
                            var config = GetStoreServerConfig(connection10);
                            if (!Directory.Exists(config.Path))
                                Directory.CreateDirectory(config.Path);
                            AddLog($"{DateTime.Now:HH:mm:ss.fff} 10附件目录：({config.StoreServerConfigID}){config.Path}");

                            //分页查询印章图
                            var names = SealNames.Replace(Environment.NewLine, ",").Replace("'", "''").Replace(",", "','");
                            if (!string.IsNullOrEmpty(names))
                            {
                                names = " WHERE MarkName IN ('" + names + "')";
                            }
                            var offset = 0;
                            while (true)
                            {
                                if (token.IsCancellationRequested) return;
                                AddLog($"{DateTime.Now:HH:mm:ss.fff} 查询07印章图 {offset + 1}-{offset + PageSize}");
                                var sql = $"SELECT MarkName, MarkBody, PassWord FROM dbo.ssMark {names} ORDER BY MarkID OFFSET {offset} ROWS FETCH NEXT {PageSize} ROWS ONLY";
                                using (var reader = connection09.ExecuteReaderAsync(new CommandDefinition(sql, cancellationToken: token)).Result)
                                {
                                    MainWindowVM.ShowStatus($"{DateTime.Now - start}");

                                    if (token.IsCancellationRequested) return;
                                    if (reader.IsClosed || reader.FieldCount == 0)
                                    {
                                        MainWindowVM.NotifyWarn("查询07印章图失败");
                                    }

                                    var table = new DataTable();
                                    table.Load(reader);
                                    AddLog($"{DateTime.Now:HH:mm:ss.fff} 获取印章图数量：{table.Rows.Count}");
                                    if (table.Rows.Count == 0)
                                        break;
                                    foreach (DataRow row in table.Rows)
                                    {
                                        try
                                        {
                                            UploadSealAttachment(connection10, config, row);
                                        }
                                        catch (Exception ex)
                                        {
                                            AddLog($"{DateTime.Now:HH:mm:ss.fff} >>失败");
                                            if (IsStopWhenError)
                                                throw;
                                            else
                                            {
                                                MainWindowVM.NotifyError(ex.Message);
                                                Logger.Error(ex.Message, ex);
                                            }
                                        }
                                    }
                                    if (table.Rows.Count < PageSize)
                                        break;
                                    offset += table.Rows.Count;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog($"{DateTime.Now:HH:mm:ss.fff} >>停止");
                    Doing = false;
                    MainWindowVM.NotifyError(ex.Message);
                    Logger.Error(ex.Message, ex);
                }
            }, token);
            task.ContinueWith((t) =>
            {
                AddLog($"{DateTime.Now:HH:mm:ss.fff} >>结束");
                Doing = false;
                MainWindowVM.ShowStatus($"{DateTime.Now - start}");
            });
        }
        private void Stop()
        {
            AddLog($"{DateTime.Now:HH:mm:ss.fff} >>停止执行");
            cts?.Cancel();
            Doing = false;
        }
        private void AddLog(string log, bool newline = true)
        {
            LogContent += log + (newline ? Environment.NewLine : string.Empty);
        }

        private dynamic GetStoreServerConfig(IDbConnection connection10)
        {
            //查附件目录、附件存储ID、附件后缀
            var sql = $@"
DECLARE @StoreServerConfigID INT, @Suffix varchar(50)
SELECT @StoreServerConfigID = ModuleInfos.StoreServerConfigID, @Suffix = StoreServerConfigs.Suffix
FROM [{AttachmentDBName}].[dbo].[ModuleInfos]
JOIN [{AttachmentDBName}].[dbo].[StoreServerConfigs] ON StoreServerConfigs.ID = ModuleInfos.StoreServerConfigID
WHERE ModuleInfos.Code = 'Portal$UnderLineImg'

INSERT INTO [{AttachmentDBName}].[dbo].[DirectoryInfos](DirectoryPath,StoreServerConfigID)
SELECT TOP(1) '1\', @StoreServerConfigID
FROM [{AttachmentDBName}].[dbo].[DirectoryInfos]
WHERE NOT EXISTS(SELECT 1 FROM [{AttachmentDBName}].[dbo].[DirectoryInfos] WHERE StoreServerConfigID = @StoreServerConfigID)

SELECT TOP(1) @StoreServerConfigID StoreServerConfigID, @Suffix Suffix, StoreServerConfigs.Root + '\' + ISNULL(DirectoryInfos.DirectoryPath, '1\') Path
FROM [{AttachmentDBName}].[dbo].[StoreServerConfigs]
JOIN [{AttachmentDBName}].[dbo].[DirectoryInfos] ON DirectoryInfos.StoreServerConfigID = StoreServerConfigs.ID
WHERE StoreServerConfigs.ID = @StoreServerConfigID";
            var config = connection10.Query<dynamic>(new CommandDefinition(sql, cancellationToken: token)).FirstOrDefault();
            return config;
        }
        private int UploadSealAttachment(IDbConnection connection10, dynamic config, DataRow data)
        {
            if (data["MarkBody"] == DBNull.Value || data["MarkBody"] is null)
                return -1;
            string extension = "png", contentType = "image/png";
            string suffix = string.IsNullOrEmpty(config.Suffix) ? extension : config.Suffix;
            var fileRename = Guid.NewGuid().ToString();
            var sign09 = new { MarkName = data["MarkName"].ToString().Replace("'", "''"), MarkBody = (byte[])data["MarkBody"], PassWord = data["PassWord"]?.ToString() };
            var fileName = Path.Combine(config.Path, fileRename + "." + suffix);
            AddLog($"{DateTime.Now:HH:mm:ss.fff} >保存图片({sign09.MarkName})：{fileName}");
            File.WriteAllBytes(fileName, sign09.MarkBody);
            AddLog($"{DateTime.Now:HH:mm:ss.fff} >上传附件({sign09.MarkName})");
            //新增印章图附件数据
            var sql = $@"
INSERT INTO [{AttachmentDBName}].[dbo].[AttachmentInfosDocument]([FileName], [Extension], [Size], [HashCode], [FileReName], [UploadTime]
,[UploaderID], [UploaderName], [Status], [ContentType], [ObjectID], [DirectoryID], [SortNum], [Compress])
SELECT N'{sign09.MarkName}' -- FileName - nvarchar(300)
,N'.{extension}' -- Extension - nvarchar(50)
,{sign09.MarkBody.Length} -- Size - float
,N'{GetFileMD5(sign09.MarkBody)}' -- HashCode - nchar(32)
,'{fileRename}' -- FileReName - uniqueidentifier
,GETDATE() -- UploadTime - datetime
,1 -- UploaderID - nvarchar(50)
,'administrator' -- UploaderName - nvarchar(50)
,2 -- Status - int
,'{contentType}' -- ContentType - varchar(100)
,N'Seal' -- ObjectID - nvarchar(100)
,{config.StoreServerConfigID} -- DirectoryID - int
,0 -- SortNum - int
,0 -- Compress - bit
WHERE NOT EXISTS(SELECT 1 FROM [{AttachmentDBName}].[dbo].[AttachmentInfosDocument] WHERE ObjectID = 'Seal' AND [FileName]=N'{sign09.MarkName}')";
            if (IsOverwrite)
            {
                //更新已有印章图附件数据
                sql += $@"
UPDATE [{AttachmentDBName}].[dbo].[AttachmentInfosDocument]
SET FileName = N'{sign09.MarkName}'
, Extension = N'.{extension}'
, [Size] = {sign09.MarkBody.Length}
, HashCode = N'{GetFileMD5(sign09.MarkBody)}'
, FileReName = '{fileRename}'
, UploadTime = GETDATE()
, UploaderID = 1
, UploaderName = 'administrator'
, [Status] = 2
, ContentType = '{contentType}'
, DirectoryID = {config.StoreServerConfigID}
, Compress = 0
, SymmetricKey = NULL
, InitializationVector = NULL
WHERE AttachmentInfosDocument.ObjectID = 'Seal' AND [FileName]=N'{sign09.MarkName}'
";
            }
            sql += $@" INSERT INTO dbo.DocumentSeal(Name, Password, SealObjectId, CreateTime, CreatorID, Creator, [Key], SortID)
SELECT N'{sign09.MarkName}' -- Name - nvarchar(50)
,N'{Base.Crypto.MD5.GetTextHash(sign09.PassWord)}' -- Password - nvarchar(100)
,(SELECT ID FROM [{AttachmentDBName}].dbo.AttachmentInfosDocument WHERE ObjectID = 'Seal' AND FileName = '{sign09.MarkName}') -- SealObjectId - int
,GETDATE() -- CreateTime - datetime
,1 -- CreatorID - int
,N'administrator' -- Creator - nvarchar(50)
,NULL -- Key - nvarchar(50)
,0 -- SortID - int
WHERE NOT EXISTS(SELECT 1 FROM dbo.DocumentSeal WHERE Name = N'{sign09.MarkName}')";
            var result = connection10.Execute(new CommandDefinition(sql, cancellationToken: token));
            if (result <= 0)
                AddLog($"{DateTime.Now:HH:mm:ss.fff} >跳过附件，印章({sign09.MarkName})未插入{(IsOverwrite ? string.Empty : "，或已有印章图")}");
            return result;
        }
        /// <summary>
        /// 获取文件MD5编码
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <returns></returns>
        private string GetFileMD5(byte[] stream)
        {
            if (stream?.Any() != true) return string.Empty;

            //创建MD5实例
            var md5Hasher = System.Security.Cryptography.MD5.Create();

            //计算MD5
            byte[] data = md5Hasher.ComputeHash(stream);

            //创建可变字符串
            StringBuilder sBuilder = new StringBuilder();

            //连接每一个 byte 的 hash 码，并格式化为十六进制字符串
            int i;
            for (i = 0; i <= (data.Length - 1); i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return sBuilder.ToString();
        }
        #endregion

        #region Setting
        public void ReadSetting()
        {
            ConnectionString07 = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(ConnectionString07)}", ConnectionString07);
            ConnectionString10 = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(ConnectionString10)}", ConnectionString10);
            IsOverwrite = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(IsOverwrite)}", IsOverwrite);
            IsStopWhenError = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(IsStopWhenError)}", IsStopWhenError);
            PageSize = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(PageSize)}", PageSize);
            AttachmentDBName = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(AttachmentDBName)}", AttachmentDBName);
            PrivilegeDBName = MainWindowVM.ReadSetting($"{nameof(i07SealImg)}.{nameof(PrivilegeDBName)}", PrivilegeDBName);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(ConnectionString07)}", ConnectionString07);
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(ConnectionString10)}", ConnectionString10);
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(IsOverwrite)}", IsOverwrite);
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(IsStopWhenError)}", IsStopWhenError);
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(PageSize)}", PageSize);
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(AttachmentDBName)}", AttachmentDBName);
            MainWindowVM.WriteSetting($"{nameof(i07SealImg)}.{nameof(PrivilegeDBName)}", PrivilegeDBName);
        }
        #endregion
    }
}
