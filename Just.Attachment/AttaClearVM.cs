using Dapper;
using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Utils;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace Just.Attachment
{
    //TODO: 多种存储方式改成接口+工厂
    [AddINotifyPropertyChangedInterface]
    public class AttaClearVM
    {
        #region 属性
        public string CfgFile { get; set; }
        public bool Backup { get; set; } = true;
        public string BackupFolder { get; set; }
        public AttaItemNode Data { get; set; } = new AttaItemNode { IsKeep = true };
        public bool Preview { get; set; } = true;
        //只扫描启用的存储空间
        public bool OnlyEnable { get; set; } = true;
        public bool Doing => Status == ActionStatus.Doing;

        public ActionStep Step { get; set; } = ActionStep.Scan;
        public ActionStatus Status { get; set; } = ActionStatus.Begin;
        public string ActionName
        {
            get
            {
                switch (Status)
                {
                    case ActionStatus.Begin:
                    case ActionStatus.Finished:
                        return Step.ToDescription();
                    case ActionStatus.Doing:
                        return "停止";
                    default:
                        return "开始";
                }
            }
        }
        #endregion

        #region 选择
        private ICommand _CfgFileBrowser;
        public ICommand CfgFileBrowser
        {
            get
            {
                _CfgFileBrowser = _CfgFileBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        InitialDirectory = Dialog.GetInitialDirectory(CfgFile)
                    };
                    dlg.Filters.Add(new CommonFileDialogFilter("配置文件", "config"));
                    dlg.Filters.Add(new CommonFileDialogFilter("所有文件", "*"));

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.CfgFile = dlg.FileName;
                    }
                });
                return _CfgFileBrowser;
            }
        }
        private ICommand _BackupFolderBrowser;
        public ICommand BackupFolderBrowser
        {
            get
            {
                _BackupFolderBrowser = _BackupFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true,
                        InitialDirectory = Dialog.GetInitialDirectory(BackupFolder)
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.BackupFolder = dlg.FileName;
                    }
                });
                return _BackupFolderBrowser;
            }
        }
        private ICommand _OpenBackupFolder;
        public ICommand OpenBackupFolder
        {
            get
            {
                _OpenBackupFolder = _OpenBackupFolder ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    OpenFolder(BackupFolder);
                });
                return _OpenBackupFolder;
            }
        }
        #endregion

        #region 执行
        private IEnumerable<string> dbAttas;
        private IStore store;
        private CancellationTokenSource tokenSource;
        private bool CheckCancel()
        {
            if (!tokenSource.IsCancellationRequested)
                return false;
            MainWindowVM.DispatcherInvoke(() =>
            {
                NotifyWin.Warn("停止" + Step.ToDescription());
                Status = ActionStatus.Begin;
                MainWindowVM.ShowStatus("停止");
            });
            return true;
        }
        private IStore GetStore(StoreServerConfig config)
        {
            IStore result;
            if (config == null)
                return null;
            switch (config?.StoreMode)
            {
                case StoreModeEnum.Local:
                    result = new LocalStore();
                    break;
                case StoreModeEnum.Share:
                    result = new ShareStore();
                    break;
                case StoreModeEnum.Ftp:
                    result = new FtpStore();
                    break;
                default:
                    MainWindowVM.NotifyWarn($"存储空间ID={config.ID}，模式={config.StoreMode}", "存储模式未支持");
                    return null;
            }
            if (!result.Init(config))
            {
                return null;
            }
            return result;
        }

        private ICommand _DoAction;
        public ICommand DoAction
        {
            get
            {
                _DoAction = _DoAction ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        switch (Step)
                        {
                            case ActionStep.Scan:
                                switch (Status)
                                {
                                    case ActionStatus.Begin:
                                        Scan();
                                        break;
                                    case ActionStatus.Doing:
                                        tokenSource?.Cancel();
                                        MainWindowVM.ShowStatus("停止...");
                                        break;
                                    case ActionStatus.Finished:
                                        Clear();
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case ActionStep.Clear:
                                switch (Status)
                                {
                                    case ActionStatus.Begin:
                                        Clear();
                                        break;
                                    case ActionStatus.Doing:
                                        tokenSource?.Cancel();
                                        MainWindowVM.ShowStatus("停止...");
                                        break;
                                    case ActionStatus.Finished:
                                        Scan();
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("清理附件错误", ex);
                        MainWindowVM.NotifyError("执行错误：" + ex.Message);
                    }
                });
                return _DoAction;
            }
        }
        #endregion

        #region 扫描
        public void Scan()
        {
            Step = ActionStep.Scan;
            Status = ActionStatus.Doing;
            if (string.IsNullOrEmpty(CfgFile))
            {
                MainWindowVM.NotifyWarn("请先选择db.config文件");
                Status = ActionStatus.Begin;
                return;
            }
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                try
                {
                    /*
                     * <iOffice10.Data.Config>
                     *   <DataAccesses>
                     *     <DataAccess module="iOffice10.Attachment">
                     *       <provider type="SqlServer" />
                     *       <connections>
                     *         <mainDataServer connectionString="Data Source =.; Initial Catalog = iOffice10.Attachment; User ID = sa; Password = asdASD!@#;" />
                     *       </connections>
                     *     </DataAccess>
                     *   </DataAccesses>
                     * </iOffice10.Data.Config>
                     * */
                    var cfg = XDocument.Load(CfgFile);
                    var dataAccess = cfg.Descendants("DataAccess").FirstOrDefault(e => e.Attribute("module").Value == "iOffice10.Attachment");
                    dataAccess = dataAccess ?? cfg.Descendants("DataAccess").FirstOrDefault(e => e.Attribute("module").Value == "Default");
                    var connectionString = dataAccess.Element("connections")?.Elements().FirstOrDefault()?.Attribute("connectionString")?.Value;
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        MainWindowVM.NotifyWarn("读取附件数据库链接失败");
                        Status = ActionStatus.Begin;
                        return;
                    }

                    MainWindowVM.DispatcherInvoke(() => { this.Data = new AttaItemNode { IsKeep = true }; });

                    using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
                    {
                        MainWindowVM.ShowStatus($"读取附件数据...");
                        dbAttas = GetDBAttachments(connection);
                        var stores = GetStoreServerConfigs(connection);
                        foreach (var store in stores)
                        {
                            ScanStore(store);
                        }
                    }

                    if (tokenSource.IsCancellationRequested)
                    {
                        Status = ActionStatus.Begin;
                        MainWindowVM.NotifyWarn("停止扫描");
                    }
                    else
                    {
                        Status = ActionStatus.Finished;
                        MainWindowVM.NotifyInfo("扫描完成");
                        Step = ActionStep.Clear;
                        Status = ActionStatus.Begin;
                        if (!Preview)
                        {
                            DoAction.Execute(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Status = ActionStatus.Begin;
                    Logger.Error("扫描附件错误", ex);
                    MainWindowVM.NotifyError(ex.Message, "扫描附件错误");
                }
                finally
                {
                    MainWindowVM.ShowStatus();
                }
            }, tokenSource.Token);
        }
        private IEnumerable<string> GetDBAttachments(IDbConnection connection)
        {
            var sql = "SELECT name FROM sys.tables WHERE name LIKE 'AttachmentInfos%'";
            var tables = connection.Query<string>(sql);
            var sqls = tables.Select(t => $@"
    SELECT [{t}].ID, FileReName, Extension, Suffix, DirectoryPath, Root, StoreMode, Server, UserName, Password, Domain
    FROM dbo.[{t}]
         JOIN dbo.DirectoryInfos ON DirectoryInfos.ID = [{t}].DirectoryID
         JOIN dbo.StoreServerConfigs ON StoreServerConfigs.ID = DirectoryInfos.StoreServerConfigID"
            );
            sql = string.Join(" UNION ALL ", sqls);
            var attas = connection.Query<AttachmentInfo>(sql);
            var result = attas.Select(atta => atta.FileReName.ToString()).Cast<string>();
            return result;
        }
        private IEnumerable<StoreServerConfig> GetStoreServerConfigs(IDbConnection connection)
        {
            var sql = "SELECT [ID], [Server], [Root], [StoreMode], [Username], [Password], [Domain], [Suffix], [IsEnabled] FROM[iOffice10.Attachment].[dbo].[StoreServerConfigs]";
            if (OnlyEnable)
                sql += "WHERE IsEnabled=1";
            var result = connection.Query<StoreServerConfig>(sql);
            return result;
        }
        private AttaItemNode ScanStore(StoreServerConfig config)
        {
            MainWindowVM.ShowStatus($"扫描存储...{config.Root}");
            try
            {
                store = GetStore(config);
                if (store == null)
                    return null;
                var node = ScanFolder(config.Path, Data);
                return node;
            }
            catch (Exception ex)
            {
                Logger.Error("扫描存储错误", ex);
                MainWindowVM.NotifyError($"{config.Root} - {ex.Message}", "扫描存储错误");
                return null;
            }
        }

        private AttaItemNode ScanFolder(string folder, AttaItemNode parent)
        {
            MainWindowVM.ShowStatus($"扫描目录...{folder}");
            var folders = store.GetDirectories(folder).ToList();
            var files = store.GetFiles(folder).ToList();
            var folderItem = new AttaItemNode
            {
                IsFolder = true,
                Path = folder,
                IsKeep = false,
                IsExpanded = parent == Data,
                FileCount = files.Count,
                FolderCount = folders.Count,
                Config = store.Config
            };

            var items = new List<AttaItemNode>();
            while (folders.Any())
            {
                if (tokenSource.IsCancellationRequested) break;

                var childItem = ScanFolder(folders.First(), folderItem);
                if (folderItem.IsKeep != childItem.IsKeep)
                {
                    folderItem.IsKeep = folderItem.Children.Count > 1 ? null : childItem.IsKeep;
                }
                folders.RemoveAt(0);
            }
            items.AddRange(folderItem.Children);

            while (files.Any())
            {
                if (tokenSource.IsCancellationRequested) break;

                var file = files.First();
                var childItem = NewFileItem(file);
                if (folderItem.IsKeep != childItem.IsKeep)
                {
                    folderItem.IsKeep = items.Any() ? null : childItem.IsKeep;
                }
                items.Add(childItem);
                files.RemoveAt(0);
            }
            //统计信息
            parent.FileCount += folderItem.FileCount;
            parent.FolderCount += folderItem.FolderCount;
            folderItem.UpdateCountInfo();

            //排序:文件夹在前>按修改时间倒序>按名称
            folderItem.Children = new ObservableCollection<AttaItemNode>(
                items.OrderByDescending(item => item.IsFolder)
                .ThenByDescending(item => item.UpdateTime)
                .ThenBy(item => item.Name));
            MainWindowVM.DispatcherInvoke(() => { parent.Children.Add(folderItem); });

            return folderItem;
        }
        private AttaItemNode NewFileItem(string file, bool? keep = null)
        {
            var item = new AttaItemNode
            {
                Path = file,
                UpdateTime = store.GetWriteTime(file)?.ToString("yyyy-MM-dd HH:mm"),
                IsKeep = IsKeepFile(file),
                Config = store.Config
            };
            if (keep.HasValue) item.IsKeep = keep.Value;
            return item;
        }
        private bool IsKeepFile(string file)
        {
            if (dbAttas == null) return false;
            if (!dbAttas.Contains(Path.GetFileNameWithoutExtension(file), StringComparer.OrdinalIgnoreCase)) return false;
            return true;
        }
        #endregion

        #region 清理
        public void Clear()
        {
            Step = ActionStep.Clear;
            Status = ActionStatus.Doing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                bool? isConfirm = MainWindowVM.MessageConfirm("即将删除残留附件文件，是否确定？");
                if (isConfirm != true)
                {
                    Status = ActionStatus.Begin;
                    return;
                }
                if (Backup)
                {
                    if (string.IsNullOrWhiteSpace(BackupFolder))
                    {
                        Status = ActionStatus.Begin;
                        MainWindowVM.NotifyWarn($"备份目录不能为空");
                        return;
                    }
                    try
                    {
                        Directory.CreateDirectory(BackupFolder);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"创建备份目录失败:{BackupFolder}", ex);
                        Status = ActionStatus.Begin;
                        MainWindowVM.NotifyError($"创建备份目录失败:{BackupFolder}\n{ex.Message}");
                        return;
                    }
                }
                Clear(Data);
                if (tokenSource.IsCancellationRequested)
                {
                    Status = ActionStatus.Begin;
                    MainWindowVM.NotifyWarn("停止清理");
                }
                else
                {
                    Status = ActionStatus.Finished;
                    MainWindowVM.NotifyInfo("清理完成");
                    Step = ActionStep.Scan;
                    Status = ActionStatus.Begin;
                }
                MainWindowVM.ShowStatus();
            }, tokenSource.Token);
        }
        private void Clear(AttaItemNode fileItem, string rootFolder = null)
        {
            var i = 0;
            while (i < fileItem.Children.Count)
            {
                if (tokenSource.IsCancellationRequested) break;

                var child = fileItem.Children[i];
                if(fileItem == Data)
                {
                    store = GetStore(child.Config);
                }
                if (string.IsNullOrEmpty(rootFolder))
                {
                    if (string.IsNullOrEmpty(fileItem.Path)) rootFolder = string.Empty;
                    rootFolder = store.GetParentDirectory(fileItem.Path);
                }
                //1.保留 则不处理, 2.未知 则处理子项 3.不保留 则删除
                if (!child.IsKeep.HasValue)
                {
                    //数量统计:先全部减去,再加剩余数量
                    fileItem.FolderCount -= child.FolderCount;
                    fileItem.FileCount -= child.FileCount;
                    Clear(child, rootFolder);
                    fileItem.FolderCount += child.FolderCount;
                    fileItem.FileCount += child.FileCount;
                    if (!child.Children.Any(f => !f.IsKeep ?? false))
                        child.IsKeep = true;
                    i++;
                }
                else if (child.IsKeep.Value)
                {
                    i++;
                }
                else
                {
                    MainWindowVM.ShowStatus($"清理...{fileItem.Path}");
                    try
                    {
                        var bak = child.Path;
                        if (!string.IsNullOrEmpty(rootFolder) && bak.StartsWith(rootFolder))
                            bak = bak.Remove(0, rootFolder.Length).TrimStart('\\', '/');
                        bak = Path.Combine(BackupFolder, bak).Replace('/', '\\');
                        if (store.DirectoryExists(child.Path))
                        {
                            if (Backup)
                            {
                                store.MoveDirectory(child.Path, bak);
                            }
                            else
                            {
                                store.DeleteDirectory(child.Path);
                            }
                        }
                        else if (store.FileExists(child.Path))
                        {
                            if (Backup)
                            {
                                store.MoveFile(child.Path, bak);
                            }
                            else
                            {
                                store.DeleteFile(child.Path);
                            }
                        }
                        if (child.IsFolder)
                        {
                            fileItem.FolderCount -= child.FolderCount + 1;//包括子文件夹本身
                            fileItem.FileCount -= child.FileCount;
                        }
                        else
                        {
                            fileItem.FileCount--;
                        }
                        MainWindowVM.DispatcherInvoke(() => { fileItem.Children.Remove(child); });
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error($"清理补丁文件错误:{child.Path}", ex);
                        MainWindowVM.NotifyError($"{child.Path}\n{ex.Message}");
                        i++;
                    }
                }

                if (fileItem == Data)
                {
                    store?.Dispose();
                    store = null;
                }
            }
            fileItem.UpdateCountInfo();
        }
        #endregion

        #region 菜单
        static string _findText = string.Empty;
        private ICommand _Find;
        public ICommand Find
        {
            get
            {
                _Find = _Find ?? new RelayCommand<AttaItemNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        var text = MessageWin.Input(_findText);
                        if (string.IsNullOrEmpty(text)) return;
                        _findText = text;
                        var result = FindNextItem(_, _findText);
                        if (result == null)
                        {
                            NotifyWin.Warn("未找到任何结果", "查找");
                        }
                    });
                });
                return _Find;
            }
        }
        private ICommand _FindNext;
        public ICommand FindNext
        {
            get
            {
                _FindNext = _FindNext ?? new RelayCommand<AttaItemNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    var result = FindNextItem(_, _findText);
                    if (result == null)
                    {
                        MainWindowVM.NotifyWarn("未找到任何结果", "查找");
                    }
                });
                return _FindNext;
            }
        }
        private AttaItemNode GetSelectedItem(AttaItemNode node = null)
        {
            node = node ?? Data;
            if (node.IsSelected) return node;
            if (node.Children?.Any() ?? false)
            {
                foreach (var child in node.Children)
                {
                    var result = GetSelectedItem(child);
                    if (result != null) return result;
                }
            }
            return null;
        }
        private AttaItemNode FindNextItem(AttaItemNode item, string findText)
        {
            item = item ?? Data;
            AttaItemNode result = null;
            if (item.Children?.Any() ?? false)
            {
                foreach (var child in item.Children)
                {
                    result = FindItem(child, findText);
                    if (result != null)
                    {
                        item.IsExpanded = true;
                        return result;
                    }
                }
            }
            return FindParentNext(item, findText);
        }
        private AttaItemNode FindParentNext(AttaItemNode item, string findText)
        {
            AttaItemNode result = null;
            var parent = GetParentItem(item);
            if (parent != null)
            {
                var startIndex = parent.Children.IndexOf(item) + 1;
                for (int i = startIndex; i < parent.Children.Count; i++)
                {
                    var child = parent.Children[i];
                    result = FindItem(child, findText);
                    if (result != null)
                    {
                        parent.IsExpanded = true;
                        return result;
                    }
                }
                return FindParentNext(parent, findText);
            }
            return result;
        }
        private AttaItemNode GetParentItem(AttaItemNode item, AttaItemNode node = null)
        {
            AttaItemNode result = null;
            node = node ?? Data;
            foreach (var child in node.Children)
            {
                if (child.Equals(item)) return node;
                result = GetParentItem(item, child);
                if (result != null) return result;
            }
            return null;
        }
        private AttaItemNode FindItem(AttaItemNode item, string findText)
        {
            AttaItemNode result = null;
            if (item.Name?.ToLower().Contains(findText.ToLower()) ?? false)
            {
                item.IsSelected = true;
                return item;
            }
            if (item.Children?.Any() ?? false)
            {
                foreach (var child in item.Children)
                {
                    result = FindItem(child, findText);
                    if (result != null)
                    {
                        item.IsExpanded = true;
                        return result;
                    }
                }
            }
            return result;
        }

        private ICommand _OpenLocation;
        public ICommand OpenLocation
        {
            get
            {
                _OpenLocation = _OpenLocation ?? new RelayCommand<AttaItemNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    OpenFolder(_?.Path, true);
                });
                return _OpenLocation;
            }
        }
        private ICommand _CopyPath;
        public ICommand CopyPath
        {
            get
            {
                _CopyPath = _CopyPath ?? new RelayCommand<AttaItemNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (string.IsNullOrEmpty(_?.Path)) return;
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        Clipboard.SetText(_.Path);
                        NotifyWin.Info(_.Path, "复制路径");
                    });
                });
                return _CopyPath;
            }
        }
        private ICommand _CopyName;
        public ICommand CopyName
        {
            get
            {
                _CopyName = _CopyName ?? new RelayCommand<AttaItemNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (string.IsNullOrEmpty(_?.Name)) return;
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        Clipboard.SetText(_.Name);
                        NotifyWin.Info(_.Name, "复制文件名");
                    });
                });
                return _CopyName;
            }
        }

        private bool OpenFolder(string folder, bool select = false)
        {
            if (string.IsNullOrEmpty(folder)) return false;
            if (!Directory.Exists(folder) && !File.Exists(folder))
            {
                MainWindowVM.NotifyWarn(folder, "路径不存在");
                return false;
            }
            if (select)
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/e,/select,\"{folder}\"");
            }
            else
            {
                System.Diagnostics.Process.Start("explorer.exe", folder);
            }
            return true;
        }
        #endregion

        #region Setting
        public void ReadSetting()
        {
            CfgFile = MainWindowVM.ReadSetting($"{nameof(AttaClearCtrl)}.{nameof(CfgFile)}", CfgFile);
            Backup = MainWindowVM.ReadSetting($"{nameof(AttaClearCtrl)}.{nameof(Backup)}", Backup);
            BackupFolder = MainWindowVM.ReadSetting($"{nameof(AttaClearCtrl)}.{nameof(BackupFolder)}", BackupFolder);
            OnlyEnable = MainWindowVM.ReadSetting($"{nameof(AttaClearCtrl)}.{nameof(OnlyEnable)}", OnlyEnable);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(AttaClearCtrl)}.{nameof(CfgFile)}", CfgFile);
            MainWindowVM.WriteSetting($"{nameof(AttaClearCtrl)}.{nameof(Backup)}", Backup);
            MainWindowVM.WriteSetting($"{nameof(AttaClearCtrl)}.{nameof(BackupFolder)}", BackupFolder);
            MainWindowVM.WriteSetting($"{nameof(AttaClearCtrl)}.{nameof(OnlyEnable)}", OnlyEnable);
        }
        #endregion

        #region Entity
        public enum ActionStatus
        {
            Begin,
            Doing,
            Finished
        }
        public enum ActionStep
        {
            [Description("扫描")]
            Scan,
            [Description("清理")]
            Clear
        }
        #endregion
    }
}
