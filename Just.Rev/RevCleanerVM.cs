using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Just.Rev
{
    [AddINotifyPropertyChangedInterface]
    public class RevCleanerVM
    {
        #region 属性
        private string _WebRootFolder = Directory.GetCurrentDirectory();
        public string WebRootFolder
        {
            get => _WebRootFolder;
            set
            {
                _WebRootFolder = value;
                if (Step != ActionStep.Scan)
                {
                    Step = ActionStep.Scan;
                    Data = new RevFileItem { IsKeep = true };
                }
            }
        }
        public bool Preview { get; set; } = true;
        public bool Backup { get; set; } = true;
        public string BackupFolder { get; set; }
        public RevFileItem Data { get; set; } = new RevFileItem { IsKeep = true };

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

        #region 方法
        private ICommand _WebRootFolderBrowser;
        public ICommand WebRootFolderBrowser
        {
            get
            {
                _WebRootFolderBrowser = _WebRootFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.WebRootFolder = dlg.FileName;
                    }
                });
                return _WebRootFolderBrowser;
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
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.BackupFolder = dlg.FileName;
                    }
                });
                return _BackupFolderBrowser;
            }
        }


        private ICommand _RevAction;
        public ICommand RevAction
        {
            get
            {
                _RevAction = _RevAction ?? new RelayCommand<RoutedEventArgs>(_ =>
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
                    catch (System.Exception ex)
                    {
                        Logger.Error("清理补丁错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("执行错误：" + ex.Message); });
                    }
                });
                return _RevAction;
            }
        }

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

        #region Scan
        private void Scan()
        {
            Step = ActionStep.Scan;
            Status = ActionStatus.Doing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                MainWindowVM.ShowStatus("扫描初始化...");
                dist = Path.Combine(WebRootFolder, "dist");
                if (!Directory.Exists(dist))
                {
                    MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("找不到dist文件夹"); });
                    MainWindowVM.ShowStatus();
                    Status = ActionStatus.Begin;
                    return;
                }
                revmanifest = Path.Combine(dist, "rev-manifest.json");
                if (!File.Exists(revmanifest))
                {
                    MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("找不到dist/rev-manifest.json文件"); });
                    MainWindowVM.ShowStatus();
                    Status = ActionStatus.Begin;
                    return;
                }
                MainWindowVM.DispatcherInvoke(() => { Data = new RevFileItem(); });
                MainWindowVM.ShowStatus("读取映射...");
                dic = ReadRevmanifest(revmanifest);
                ScanFolder(dist, Data);
                if (tokenSource.IsCancellationRequested)
                {
                    Status = ActionStatus.Begin;
                    MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("停止扫描"); });
                }
                else
                {
                    Status = ActionStatus.Finished;
                    MainWindowVM.DispatcherInvoke(() => { NotifyWin.Info("扫描完成"); });
                    Step = ActionStep.Clear;
                    Status = ActionStatus.Begin;
                    if (!Preview)
                    {
                        RevAction.Execute(null);
                    }
                }
                MainWindowVM.ShowStatus();
            }, tokenSource.Token);
        }
        private Dictionary<string, string> ReadRevmanifest(string fileName)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            var lines = File.ReadAllLines(fileName, System.Text.Encoding.UTF8);
            foreach (var line in lines)
            {
                var text = line.Trim().Trim('{', '}', ',');
                if (string.IsNullOrEmpty(text))
                    continue;
                var kv = text.Split(':');
                var v = kv[0].Trim().Trim('"', ' ').ToLower();
                var k = kv[1].Trim().Trim('"', ' ').ToLower();
                d.Add(k, v);
            }
            return d;
        }

        private string dist;
        private string revmanifest;
        private Dictionary<string, string> dic;
        private RevFileItem ScanFolder(string folder, RevFileItem parent)
        {
            MainWindowVM.ShowStatus($"扫描目录...{folder}");
            var folders = Directory.GetDirectories(folder).ToList();
            var files = Directory.GetFiles(folder).ToList();
            var folderItem = new RevFileItem
            {
                IsFolder = true,
                ImagePath = @"\Images\folder.png",
                Name = Path.GetFileName(folder),
                Path = folder,
                IsKeep = false,
                IsExpanded = folder == dist,
                FileCount = files.Count,
                FolderCount = folders.Count
            };

            var items = new List<RevFileItem>();
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
                bool? keep = null;
                if (file == revmanifest) keep = true;
                var childItem = NewRevFileItem(file, keep);
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
            folderItem.Children = new ObservableCollection<RevFileItem>(
                items.OrderByDescending(item => item.IsFolder)
                .ThenByDescending(item => item.UpdateTime)
                .ThenBy(item => item.Name));
            MainWindowVM.DispatcherInvoke(() => { parent.Children.Add(folderItem); });

            return folderItem;
        }

        private RevFileItem NewRevFileItem(string file, bool? keep = null)
        {
            var item = new RevFileItem { ImagePath = GetFileIcon(file), Name = Path.GetFileName(file), Path = file, UpdateTime = File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm") };
            var rev = GetRevFile(file);
            if (dic.ContainsKey(rev))
            {
                item.IsKeep = true;
                item.OrigFile = dic[rev];
                item.RevFile = rev;
            }
            else
            {
                item.IsKeep = false;
                item.OrigFile = GetOrigFile(rev);
                if (dic.ContainsKey(item.OrigFile)) item.RevFile = dic[item.OrigFile];
            }
            if (keep.HasValue) item.IsKeep = keep.Value;
            return item;
        }
        private string GetOrigFile(string file)
        {
            var result = Regex.Match(file, @"(.+)-[a-z0-9]{10}(\..+)?");
            if (!result.Success) return string.Empty;
            return result.Groups[1].Value + result.Groups[2].Value;
        }
        private string GetRevFile(string file)
        {
            var result = file.StartsWith(dist) ? file.Replace(dist, "").Trim('\\').Replace('\\', '/').ToLower() : file;
            return result;
        }

        private string GetFileIcon(string file)
        {
            var imgFolder = @"\Images\";
            var img = "file.png";
            return imgFolder + img;
        }
        public bool ImageExists(string img)
        {
            var resourcePath = img.Replace('\\', '/').TrimStart('/').ToLower();
            return ResourceExists(resourcePath);
        }
        public static bool ResourceExists(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            return ResourceExists(assembly, resourcePath);
        }
        public static bool ResourceExists(Assembly assembly, string resourcePath)
        {
            return GetResourcePaths(assembly)
                .Contains(resourcePath.ToLowerInvariant());
        }
        public static IEnumerable<object> GetResourcePaths(Assembly assembly)
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            var resourceName = assembly.GetName().Name + ".g";
            var resourceManager = new ResourceManager(resourceName, assembly);

            try
            {
                var resourceSet = resourceManager.GetResourceSet(culture, true, true);

                foreach (System.Collections.DictionaryEntry resource in resourceSet)
                {
                    yield return resource.Key;
                }
            }
            finally
            {
                resourceManager.ReleaseAllResources();
            }
        }
        #endregion

        #region Clear
        private void Clear()
        {
            Step = ActionStep.Clear;
            Status = ActionStatus.Doing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                bool? isConfirm = null;
                MainWindowVM.DispatcherInvoke(() =>
                {
                    isConfirm = MessageWin.Confirm("即将删除多余补丁文件，是否确定？");
                });
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
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn($"备份目录不能为空"); });
                        return;
                    }
                    try
                    {
                        Directory.CreateDirectory(BackupFolder);
                    }
                    catch (System.Exception ex)
                    {
                        Logger.Error($"创建备份目录失败:{BackupFolder}", ex);
                        Status = ActionStatus.Begin;
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error($"创建备份目录失败:{BackupFolder}\n{ex.Message}"); });
                        return;
                    }
                }
                Clear(Data);
                if (tokenSource.IsCancellationRequested)
                {
                    Status = ActionStatus.Begin;
                    MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("停止清理"); });
                }
                else
                {
                    Status = ActionStatus.Finished;
                    MainWindowVM.DispatcherInvoke(() => { NotifyWin.Info("清理完成"); });
                    Step = ActionStep.Scan;
                    Status = ActionStatus.Begin;
                }
                MainWindowVM.ShowStatus();
            }, tokenSource.Token);
        }
        private void Clear(RevFileItem fileItem)
        {
            var i = 0;
            while (i < fileItem.Children.Count)
            {
                if (tokenSource.IsCancellationRequested) break;

                var child = fileItem.Children[i];
                //1.保留 则不处理, 2.未知 则处理子项 3.不保留 则删除
                if (!child.IsKeep.HasValue)
                {
                    //数量统计:先全部减去,再加剩余数量
                    fileItem.FolderCount -= child.FolderCount;
                    fileItem.FileCount -= child.FileCount;
                    Clear(child);
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
                        if (Directory.Exists(child.Path))
                        {
                            if (Backup)
                            {
                                var bak = Path.Combine(BackupFolder, child.Path.Replace(WebRootFolder, "").TrimStart('\\'));
                                MoveDirectory(child.Path, bak);
                            }
                            else
                            {
                                foreach (var file in Directory.EnumerateFiles(child.Path, "*", SearchOption.AllDirectories))
                                {
                                    File.SetAttributes(file, FileAttributes.Normal);
                                }
                                Directory.Delete(child.Path, true);
                            }
                        }
                        else if (File.Exists(child.Path))
                        {
                            if (Backup)
                            {
                                var bak = Path.Combine(BackupFolder, child.Path.Replace(WebRootFolder, "").TrimStart('\\'));
                                var dir = Path.GetDirectoryName(bak);
                                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                                if (File.Exists(bak))
                                {
                                    //bak = GetFilePathNotExists(bak);
                                    File.SetAttributes(bak, FileAttributes.Normal);
                                    File.Delete(bak);
                                }
                                File.Move(child.Path, bak);
                            }
                            else
                            {
                                File.SetAttributes(child.Path, FileAttributes.Normal);
                                File.Delete(child.Path);
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
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error($"{child.Path}\n{ex.Message}"); });
                        i++;
                    }
                }
            }
            fileItem.UpdateCountInfo();
        }
        private string GetFilePathNotExists(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var index = 0;
            while (File.Exists(filePath))
            {
                index++;
                filePath = Path.Combine(dir, $"{fileName} ({index}){extension}");
            }
            return filePath;
        }
        private void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            if (sourcePath.ToLower().Equals(targetPath.ToLower())) return;

            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile))
                    {
                        File.SetAttributes(targetFile, FileAttributes.Normal);
                        File.Delete(targetFile);
                    }
                    File.Move(file, targetFile);
                }
            }
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(source, true);
        }
        #endregion

        #endregion

        #region 菜单
        static string _findText = string.Empty;
        private ICommand _Find;
        public ICommand Find
        {
            get
            {
                _Find = _Find ?? new RelayCommand<RevFileItem>(_ =>
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
                _FindNext = _FindNext ?? new RelayCommand<RevFileItem>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    var result = FindNextItem(_, _findText);
                    if (result == null)
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("未找到任何结果", "查找"); });
                    }
                });
                return _FindNext;
            }
        }
        private RevFileItem GetSelectedItem(RevFileItem node = null)
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
        private RevFileItem FindNextItem(RevFileItem item, string findText)
        {
            item = item ?? Data;
            RevFileItem result = null;
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
        private RevFileItem FindParentNext(RevFileItem item, string findText)
        {
            RevFileItem result = null;
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
        private RevFileItem GetParentItem(RevFileItem item, RevFileItem node = null)
        {
            RevFileItem result = null;
            node = node ?? Data;
            foreach (var child in node.Children)
            {
                if (child.Equals(item)) return node;
                result = GetParentItem(item, child);
                if (result != null) return result;
            }
            return null;
        }
        private RevFileItem FindItem(RevFileItem item, string findText)
        {
            RevFileItem result = null;
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
                _OpenLocation = _OpenLocation ?? new RelayCommand<RevFileItem>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (string.IsNullOrEmpty(_?.Path)) return;
                    if (!Directory.Exists(_.Path) && !File.Exists(_.Path))
                    {
                        MainWindowVM.DispatcherInvoke(() =>
                        {
                            NotifyWin.Warn(_.Path, "路径不存在");
                        });
                    }
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("Explorer.exe")
                    {
                        Arguments = $"/e,/select,\"{_.Path}\""
                    };
                    System.Diagnostics.Process.Start(psi);
                });
                return _OpenLocation;
            }
        }
        private ICommand _CopyPath;
        public ICommand CopyPath
        {
            get
            {
                _CopyPath = _CopyPath ?? new RelayCommand<RevFileItem>(_ =>
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
                _CopyName = _CopyName ?? new RelayCommand<RevFileItem>(_ =>
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
        #endregion

        #region Setting
        public void ReadSetting()
        {
            WebRootFolder = MainWindowVM.ReadSetting($"{nameof(RevCleanerCtrl)}.{nameof(WebRootFolder)}", WebRootFolder);
            Preview = MainWindowVM.ReadSetting($"{nameof(RevCleanerCtrl)}.{nameof(Preview)}", Preview);
            Backup = MainWindowVM.ReadSetting($"{nameof(RevCleanerCtrl)}.{nameof(Backup)}", Backup);
            BackupFolder = MainWindowVM.ReadSetting($"{nameof(RevCleanerCtrl)}.{nameof(BackupFolder)}", BackupFolder);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(RevCleanerCtrl)}.{nameof(WebRootFolder)}", WebRootFolder);
            MainWindowVM.WriteSetting($"{nameof(RevCleanerCtrl)}.{nameof(Preview)}", Preview);
            MainWindowVM.WriteSetting($"{nameof(RevCleanerCtrl)}.{nameof(Backup)}", Backup);
            MainWindowVM.WriteSetting($"{nameof(RevCleanerCtrl)}.{nameof(BackupFolder)}", BackupFolder);
        }
        #endregion

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
    }
}
