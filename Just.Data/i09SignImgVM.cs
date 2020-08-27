using Dapper;
using GenLibrary.MVVM.Base;
using Just.Base;
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
    public class i09SignImgVM
    {
        public string ConnectionString09 { get; set; } = "Data Source =.; Initial Catalog = iOffice; User ID = sa; Password = asdASD!@#;";
        public string ConnectionString10 { get; set; } = "Data Source =.; Initial Catalog = iOffice10; User ID = sa; Password = asdASD!@#;";
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
                        if (string.IsNullOrWhiteSpace(ConnectionString09))
                        {
                            MainWindowVM.NotifyWarn("请先配置09链接字符串");
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

                    using (IDbConnection connection09 = new System.Data.SqlClient.SqlConnection(ConnectionString09))
                    {
                        using (IDbConnection connection10 = new System.Data.SqlClient.SqlConnection(ConnectionString10))
                        {
                            AddLog($"{DateTime.Now:HH:mm:ss.fff} 查询10附件存储配置");
                            var config = GetStoreServerConfig(connection10);
                            if (!Directory.Exists(config.Path))
                                Directory.CreateDirectory(config.Path);
                            AddLog($"{DateTime.Now:HH:mm:ss.fff} 10附件目录：({config.StoreServerConfigID}){config.Path}");

                            //分页查询签名图
                            var offset = 0;
                            while (true)
                            {
                                if (token.IsCancellationRequested) return;
                                AddLog($"{DateTime.Now:HH:mm:ss.fff} 查询09签名图 {offset + 1}-{offset + PageSize}");
                                var sql = $"SELECT EmpID, EmpSign FROM dbo.ssEmpSign ORDER BY EmpID OFFSET {offset} ROWS FETCH NEXT {PageSize} ROWS ONLY";
                                using (var reader = connection09.ExecuteReaderAsync(new CommandDefinition(sql, cancellationToken: token)).Result)
                                {
                                    MainWindowVM.ShowStatus($"{DateTime.Now - start}");

                                    if (token.IsCancellationRequested) return;
                                    if (reader.IsClosed || reader.FieldCount == 0)
                                    {
                                        MainWindowVM.NotifyWarn("查询09签名图失败");
                                    }

                                    var table = new DataTable();
                                    table.Load(reader);
                                    AddLog($"{DateTime.Now:HH:mm:ss.fff} 获取签名图数量：{table.Rows.Count}");
                                    if (table.Rows.Count == 0)
                                        break;
                                    foreach (DataRow row in table.Rows)
                                    {
                                        try
                                        {
                                            UploadSignAttachment(connection10, config, row);
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
            //查附件目录和附件存储ID
            var sql = $@"
DECLARE @StoreServerConfigID INT
SELECT @StoreServerConfigID = ModuleInfos.StoreServerConfigID
FROM [{AttachmentDBName}].[dbo].[ModuleInfos]
JOIN [{AttachmentDBName}].[dbo].[StoreServerConfigs] ON StoreServerConfigs.ID = ModuleInfos.StoreServerConfigID
WHERE ModuleInfos.Code = 'Portal$UnderLineImg'

INSERT INTO [{AttachmentDBName}].[dbo].[DirectoryInfos](DirectoryPath,StoreServerConfigID)
SELECT TOP(1) '1\', @StoreServerConfigID
FROM [{AttachmentDBName}].[dbo].[DirectoryInfos]
WHERE NOT EXISTS(SELECT 1 FROM [{AttachmentDBName}].[dbo].[DirectoryInfos] WHERE StoreServerConfigID = @StoreServerConfigID)

SELECT TOP(1) @StoreServerConfigID StoreServerConfigID, StoreServerConfigs.Root + '\' + ISNULL(DirectoryInfos.DirectoryPath, '1\') Path
FROM [{AttachmentDBName}].[dbo].[StoreServerConfigs]
JOIN [{AttachmentDBName}].[dbo].[DirectoryInfos] ON DirectoryInfos.StoreServerConfigID = StoreServerConfigs.ID
WHERE StoreServerConfigs.ID = @StoreServerConfigID";
            var config = connection10.Query<dynamic>(new CommandDefinition(sql, cancellationToken: token)).FirstOrDefault();
            return config;
        }
        private int UploadSignAttachment(IDbConnection connection10, dynamic config, DataRow data)
        {
            if (data["EmpSign"] == DBNull.Value || data["EmpSign"] is null)
                return -1;
            string extension = "png", contentType = "image/png";
            var fileRename = Guid.NewGuid().ToString();
            var sign09 = new { EmpID = (int)data["EmpID"], EmpSign = (byte[])data["EmpSign"] };
            var fileName = Path.Combine(config.Path, fileRename + "." + extension);
            AddLog($"{DateTime.Now:HH:mm:ss.fff} >保存图片({sign09.EmpID})：{fileName}");
            File.WriteAllBytes(fileName, sign09.EmpSign);
            AddLog($"{DateTime.Now:HH:mm:ss.fff} >上传附件({sign09.EmpID})");
            //新增签名图附件数据
            var sql = $@"
INSERT INTO [{AttachmentDBName}].[dbo].[AttachmentInfosPortal]([FileName], [Extension], [Size], [HashCode], [FileReName], [UploadTime]
,[UploaderID], [UploaderName], [Status], [ContentType], [ObjectID], [DirectoryID], [SortNum], [Compress])
SELECT TOP(1) N'{sign09.EmpID}' -- FileName - nvarchar(300)
,N'{extension}' -- Extension - nvarchar(50)
,{sign09.EmpSign.Length} -- Size - float
,N'{GetFileMD5(sign09.EmpSign)}' -- HashCode - nchar(32)
,'{fileRename}' -- FileReName - uniqueidentifier
,GETDATE() -- UploadTime - datetime
,PriUsers.ID -- UploaderID - nvarchar(50)
,PriUsers.Name -- UploaderName - nvarchar(50)
,2 -- Status - int
,'{contentType}' -- ContentType - varchar(100)
,N'undeavatar{sign09.EmpID}' -- ObjectID - nvarchar(100)
,{config.StoreServerConfigID} -- DirectoryID - int
,0 -- SortNum - int
,0 -- Compress - bit
FROM [{PrivilegeDBName}].[dbo].[PriUsers]
WHERE PriUsers.ID = {sign09.EmpID}
AND NOT EXISTS(SELECT 1 FROM [{AttachmentDBName}].[dbo].[AttachmentInfosPortal] WHERE ObjectID = 'undeavatar{sign09.EmpID}')";
            if (IsOverwrite)
            {
                //更新已有签名图附件数据
                sql += $@"
UPDATE [{AttachmentDBName}].[dbo].[AttachmentInfosPortal]
SET FileName = N'{sign09.EmpID}'
, Extension = N'{extension}'
, [Size] = {sign09.EmpSign.Length}
, HashCode = N'{GetFileMD5(sign09.EmpSign)}'
, FileReName = '{fileRename}'
, UploadTime = GETDATE()
, UploaderID = PriUsers.ID
, UploaderName = PriUsers.Name
, [Status] = 2
, ContentType = '{contentType}'
, DirectoryID = {config.StoreServerConfigID}
, Compress = 0
, SymmetricKey = NULL
, InitializationVector = NULL
FROM [{PrivilegeDBName}].[dbo].[PriUsers]
WHERE PriUsers.ID = {sign09.EmpID}
AND AttachmentInfosPortal.ObjectID = 'undeavatar{sign09.EmpID}'
";
            }
            var result = connection10.Execute(new CommandDefinition(sql, cancellationToken: token));
            if (result <= 0)
                AddLog($"{DateTime.Now:HH:mm:ss.fff} >跳过附件，人员ID({sign09.EmpID})不存在{(IsOverwrite ? string.Empty : "或已有签名图")}");
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
            MD5 md5Hasher = MD5.Create();

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
            ConnectionString09 = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(ConnectionString09)}", ConnectionString09);
            ConnectionString10 = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(ConnectionString10)}", ConnectionString10);
            IsOverwrite = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(IsOverwrite)}", IsOverwrite);
            IsStopWhenError = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(IsStopWhenError)}", IsStopWhenError);
            PageSize = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(PageSize)}", PageSize);
            AttachmentDBName = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(AttachmentDBName)}", AttachmentDBName);
            PrivilegeDBName = MainWindowVM.ReadSetting($"{nameof(i09SignImg)}.{nameof(PrivilegeDBName)}", PrivilegeDBName);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(ConnectionString09)}", ConnectionString09);
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(ConnectionString10)}", ConnectionString10);
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(IsOverwrite)}", IsOverwrite);
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(IsStopWhenError)}", IsStopWhenError);
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(PageSize)}", PageSize);
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(AttachmentDBName)}", AttachmentDBName);
            MainWindowVM.WriteSetting($"{nameof(i09SignImg)}.{nameof(PrivilegeDBName)}", PrivilegeDBName);
        }
        #endregion
    }
}
