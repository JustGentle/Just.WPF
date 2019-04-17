using PropertyChanged;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using Just.WPF.Views;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System;
using System.Windows.Threading;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Just.WPF;
using System.Reflection;
using System.Resources;

namespace Just.WPF.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class RevCleanerSetting
    {
        #region 单例
        private static RevCleanerSetting _Instance;
        public static RevCleanerSetting Instance
        {
            get
            {
                _Instance = _Instance ?? new RevCleanerSetting();
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

        public ActionStatus Status { get; set; }
        public string ClearActionName
        {
            get
            {
                switch (Status)
                {
                    case ActionStatus.Begin:
                    case ActionStatus.Finished:
                        return "扫描";
                    case ActionStatus.Scanning:
                        return "停止";
                    case ActionStatus.Scanned:
                        return "清理";
                    case ActionStatus.Clearing:
                        return "停止";
                    default:
                        return "开始";
                }
            }
        }

        private CancellationTokenSource tokenSource;
        private ICommand _ClearAction;
        public ICommand ClearAction
        {
            get
            {
                _ClearAction = _ClearAction ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    switch (Status)
                    {
                        case ActionStatus.Begin:
                        case ActionStatus.Finished:
                            Scan();
                            break;
                        case ActionStatus.Scanning:
                            Status = ActionStatus.Begin;
                            tokenSource?.Cancel();
                            NotifyWin.Warn("停止扫描");
                            break;
                        case ActionStatus.Scanned:
                            Clear();
                            break;
                        case ActionStatus.Clearing:
                            Status = ActionStatus.Scanned;
                            tokenSource?.Cancel();
                            NotifyWin.Warn("停止清理");
                            break;
                        default:
                            break;
                    }
                });
                return _ClearAction;
            }
        }

        #region Scan
        private void Scan()
        {
            Status = ActionStatus.Scanning;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Process = total = count = 0;
                MainWindow.Instance.ShowStatus("扫描初始化...", true, Process);
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
                MainWindow.Instance.ShowStatus("读取映射...", true, Process);
                dic = ReadRevmanifest(revmanifest);
                ScanFolder(dist, Data);
                if (count == total)
                {
                    Status = ActionStatus.Scanned;
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Info("扫描完成"); });
                    if (!Preview)
                    {
                        ClearAction.Execute(null);
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
        private void ScanFolder(string folder, RevFileItem parent)
        {
            MainWindow.Instance.ShowStatus($"扫描目录...{folder}", true, Process);
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
                ScanFolder(folders.First(), folderItem);
                folders.RemoveAt(0);
                count++;
                Process = (int)(count * 100.0 / total);
            }
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
            if(ImageExists(image))
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
            Status = ActionStatus.Clearing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Process = 0;
                Clear(Data);
                Process = 100;
                if (Process == 100)
                {
                    Status = ActionStatus.Finished;
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Info("清理完成"); });
                }
            }, tokenSource.Token);
        }
        private void Clear(RevFileItem fileItem)
        {
            var i = fileItem.Children.Count;
            while(i > 0)
            {
                var child = fileItem.Children[--i];
                if (child.IsKeep)
                {
                    Clear(child);
                }
                else
                {
                    MainWindow.DispatcherInvoke(() => { fileItem.Children.Remove(child); });
                }
            }
        }
        #endregion

        #endregion

        public enum ActionStatus
        {
            Begin,
            Scanning,
            Scanned,
            Clearing,
            Finished
        }
    }
}
