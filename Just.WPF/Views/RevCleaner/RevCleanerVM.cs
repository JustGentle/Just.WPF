using PropertyChanged;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Resources;
using GenLibrary.MVVM.Base;
using System.ComponentModel;

namespace Just.WPF.Views
{
    [AddINotifyPropertyChangedInterface]
    public class RevCleanerVM
    {
        #region 单例
        private static RevCleanerVM _Instance;
        public static RevCleanerVM Instance
        {
            get
            {
                _Instance = _Instance ?? new RevCleanerVM();
                return _Instance;
            }
        }
        #endregion

        #region 属性
        public string WebRootFolder { get; set; } = Directory.GetCurrentDirectory();
        public bool Preview { get; set; } = true;
        public bool Backup { get; set; }
        public string BackupFolder { get; set; }
        public int Process { get; set; }
        public RevFileItem Data { get; set; } = new RevFileItem { IsKeep = true };
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

        private CancellationTokenSource tokenSource;
        private ICommand _RevAction;
        public ICommand RevAction
        {
            get
            {
                _RevAction = _RevAction ?? new RelayCommand<RoutedEventArgs>(_ =>
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
                                    NotifyWin.Warn("停止" + Step.ToDescription());
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
                                    NotifyWin.Warn("停止" + Step.ToDescription());
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
                });
                return _RevAction;
            }
        }

        #region Scan
        private void Scan()
        {
            Step = ActionStep.Scan;
            Status = ActionStatus.Doing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                MainWindow.Instance.ShowStatus("扫描初始化...");
                dist = Path.Combine(WebRootFolder, "dist");
                if (!Directory.Exists(dist))
                {
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Warn("找不到dist文件夹"); });
                    MainWindow.Instance.ShowStatus();
                    Status = ActionStatus.Begin;
                    return;
                }
                revmanifest = Path.Combine(dist, "rev-manifest.json");
                if (!File.Exists(revmanifest))
                {
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Warn("找不到dist/rev-manifest.json文件"); });
                    MainWindow.Instance.ShowStatus();
                    Status = ActionStatus.Begin;
                    return;
                }
                MainWindow.DispatcherInvoke(() => { Data = new RevFileItem(); });
                MainWindow.Instance.ShowStatus("读取映射...");
                dic = ReadRevmanifest(revmanifest);
                ScanFolder(dist, Data);
                if (!tokenSource.IsCancellationRequested)
                {
                    Status = ActionStatus.Finished;
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Info("扫描完成"); });
                    Step = ActionStep.Clear;
                    Status = ActionStatus.Begin;
                    if (!Preview)
                    {
                        RevAction.Execute(null);
                    }
                }
                MainWindow.Instance.ShowStatus();
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
        private int total = 0;
        private int count = 0;
        private RevFileItem ScanFolder(string folder, RevFileItem parent)
        {
            MainWindow.Instance.ShowStatus($"扫描目录...{folder}");
            var folderItem = new RevFileItem { ImagePath = @"\Images\folder.png", Name = Path.GetFileName(folder), Path = folder, IsKeep = false, IsExpanded = folder == dist };
            var folders = Directory.GetDirectories(folder).ToList();
            var files = Directory.GetFiles(folder).ToList();
            total += folders.Count() + files.Count();
            while (files.Any())
            {
                var file = files.First();
                bool? keep = null;
                if (file == revmanifest) keep = true;
                var fileItem = NewRevFileItem(file, keep);
                folderItem.IsKeep = folderItem.IsKeep || fileItem.IsKeep;
                MainWindow.DispatcherInvoke(() => { folderItem.Children.Add(fileItem); });
                files.RemoveAt(0);
                count++;
                Process = (int)(count * 100.0 / total);
            }
            MainWindow.DispatcherInvoke(() => { parent.Children.Add(folderItem); });
            while (folders.Any())
            {
                var childFolderItem = ScanFolder(folders.First(), folderItem);
                folderItem.IsKeep = folderItem.IsKeep || childFolderItem.IsKeep;
                folders.RemoveAt(0);
                count++;
                Process = (int)(count * 100.0 / total);
            }
            return folderItem;
        }

        private RevFileItem NewRevFileItem(string file, bool? keep = null)
        {
            var item = new RevFileItem { ImagePath = GetFileIcon(file), Name = Path.GetFileName(file), Path = file, UpdateTime = File.GetLastWriteTime(file).ToString("yyyy-M-d H:m") };
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
        private readonly string[] fontExts = { "otf", "oet", "svg", "ttf", "woff", "woff2", "eot" };
        private string GetFileIcon(string file)
        {
            var ext = Path.GetExtension(file).TrimStart('.');
            if (fontExts.Contains(ext))
                ext = "font";
            var image = $@"\Images\{ext}.png";
            if (ImageExists(image))
                return image;
            return $@"\Images\file.png";
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

        #endregion

        #region Clear
        private void Clear()
        {
            Step = ActionStep.Clear;
            Status = ActionStatus.Doing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                if (Backup)
                {
                    try
                    {
                        Directory.CreateDirectory(BackupFolder);
                    }
                    catch (System.Exception ex)
                    {
                        MainWindow.DispatcherInvoke(() => { NotifyWin.Error($"创建备份目录失败:{BackupFolder}\n{ex.Message}"); });
                        return;
                    }
                }
                Clear(Data);
                if (!tokenSource.IsCancellationRequested)
                {
                    Status = ActionStatus.Finished;
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Info("清理完成"); });
                    Step = ActionStep.Scan;
                    Status = ActionStatus.Begin;
                }
            }, tokenSource.Token);
        }
        private void Clear(RevFileItem fileItem)
        {
            var i = 0;
            while (i < fileItem.Children.Count)
            {
                var child = fileItem.Children[i];
                if (child.IsKeep)
                {
                    Clear(child);
                    i++;
                }
                else
                {
                    try
                    {
                        if (Directory.Exists(child.Path))
                        {
                            if (Backup)
                            {
                                var bak = Path.Combine(BackupFolder, child.Path.Replace(dist, "").TrimStart('\\'));
                                MoveDirectory(child.Path, bak);
                            }   
                            else
                                Directory.Delete(child.Path, true);
                        }
                        else if (File.Exists(child.Path))
                        {
                            if (Backup)
                            {
                                var bak = Path.Combine(BackupFolder, child.Path.Replace(dist, "").TrimStart('\\'));
                                Directory.CreateDirectory(Path.GetDirectoryName(bak));
                                File.Move(child.Path, bak);
                            }
                            else
                                File.Delete(child.Path);
                        }
                        MainWindow.DispatcherInvoke(() => { fileItem.Children.Remove(child); });
                    }
                    catch (System.Exception ex)
                    {
                        MainWindow.DispatcherInvoke(() => { NotifyWin.Error($"{child.Path}\n{ex.Message}"); });
                        i++;
                    }
                }
            }
        }

        private void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }
        #endregion

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
