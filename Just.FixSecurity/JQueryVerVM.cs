using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Utils;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Just.FixSecurity
{
    [AddINotifyPropertyChangedInterface]
    public class JQueryVerVM
    {
        #region 属性
        public string ToVersion { get; set; } = "99.9.9";
        private string _WebRootFolder = Directory.GetCurrentDirectory();
        public string WebRootFolder
        {
            get => _WebRootFolder;
            set
            {
                _WebRootFolder = value;
            }
        }
        public bool Backup { get; set; } = true;
        private string _CurBackupFolder = string.Empty;
        public string BackupFolder { get; set; }
        public JQFileItem Data { get; set; } = new JQFileItem();

        public bool Doing { get; set; } = false;

        public string ActionName
        {
            get
            {
                return Doing ? "停止" : "开始";
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


        private ICommand _DoAction;
        public ICommand DoAction
        {
            get
            {
                _DoAction = _DoAction ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        if (Doing)
                        {
                            tokenSource?.Cancel();
                            MainWindowVM.ShowStatus("停止...");
                        }
                        else
                        {
                            Scan();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("更新JQuery版本错误", ex);
                        MainWindowVM.NotifyError("执行错误：" + ex.Message);
                    }
                });
                return _DoAction;
            }
        }

        private CancellationTokenSource tokenSource;

        #region Scan
        private void Scan()
        {
            Doing = true;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                if (!CheckSetting())
                {
                    Doing = false;
                    return;
                }

                MainWindowVM.ShowStatus("执行...");
                MainWindowVM.DispatcherInvoke(() => { Data = new JQFileItem(); });
                ScanFolder(WebRootFolder, Data);
                Doing = false;
                if (tokenSource.IsCancellationRequested)
                {
                    MainWindowVM.NotifyWarn("停止执行");
                }
                else
                {
                    MainWindowVM.NotifyInfo("执行完成");
                }
                MainWindowVM.ShowStatus();
            }, tokenSource.Token);
        }

        private bool CheckSetting()
        {
            if (Backup)
            {
                if (string.IsNullOrEmpty(ToVersion))
                {
                    MainWindowVM.NotifyWarn($"目标版本不能为空");
                    return false;
                }
                if (!r_JQVersion.IsMatch($"'{ToVersion}'"))
                {
                    MainWindowVM.NotifyWarn($"目标版本格式不正确，示例：99.9.9");
                    return false;
                }
                if (string.IsNullOrEmpty(WebRootFolder))
                {
                    MainWindowVM.NotifyWarn($"站点目录不能为空");
                    return false;
                }
                if (!Directory.Exists(WebRootFolder))
                {
                    MainWindowVM.NotifyWarn($"站点目录不存在");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(BackupFolder))
                {
                    MainWindowVM.NotifyWarn($"备份目录不能为空");
                    return false;
                }
                if (BackupFolder.Contains(WebRootFolder))
                {
                    MainWindowVM.NotifyWarn($"备份目录不能在站点目录内");
                    return false;
                }
                _CurBackupFolder = Path.Combine(BackupFolder, DateTime.Now.ToString("yyyyMMddHHmmss"));
                try
                {
                    Directory.CreateDirectory(_CurBackupFolder);
                }
                catch (Exception ex)
                {
                    Logger.Error($"创建备份目录失败:{BackupFolder}", ex);
                    MainWindowVM.NotifyError($"创建备份目录失败:{BackupFolder}\n{ex.Message}");
                    return false;
                }
            }
            return true;
        }
        private JQFileItem ScanFolder(string folder, JQFileItem parent)
        {
            MainWindowVM.ShowStatus($"扫描目录...{folder}");
            var folders = Directory.GetDirectories(folder).ToList();
            var files = Directory.GetFiles(folder, "*jquery*").ToList();
            var folderItem = new JQFileItem
            {
                IsFolder = true,
                Name = Path.GetFileName(folder),
                Path = folder,
                IsExpanded = true
            };

            var items = new List<JQFileItem>();
            while (folders.Any())
            {
                if (tokenSource.IsCancellationRequested) break;

                var childItem = ScanFolder(folders.First(), folderItem);
                folders.RemoveAt(0);
            }
            items.AddRange(folderItem.Children);
            //folderItem.FolderCount += items.Count;

            while (files.Any())
            {
                if (tokenSource.IsCancellationRequested) break;

                var file = files.First();
                var childItem = DoOne(file);
                if (childItem != null)
                {
                    items.Add(childItem);
                    folderItem.FileCount++;
                }
                files.RemoveAt(0);
            }

            if (items.Count == 0)
                return null;

            //统计信息
            parent.FileCount += folderItem.FileCount;
            //parent.FolderCount += folderItem.FolderCount;
            folderItem.UpdateCountInfo();

            //排序:文件夹在前>按修改时间倒序>按名称
            folderItem.Children = new ObservableCollection<JQFileItem>(
                items.OrderByDescending(item => item.IsFolder)
                .ThenBy(item => item.Name));
            MainWindowVM.DispatcherInvoke(() => { parent.Children.Add(folderItem); });

            return folderItem;
        }

        //jquery.js jquery-2.1.4.js jquery-2.1.4.min.js
        private readonly Regex r_JQFile = new Regex(@"\bjquery\b([\W_][1-9]\b.*)?\.js$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        //v2.1.4 jquery:"2.1.4" version = "2.1.4" core_version = "2.1.4" m="2.1.4"
        private readonly Regex r_JQVersion = new Regex(@"(?<=""|'|v)([0-9]+(\.[0-9]+)+)(?=""|'|\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private JQFileItem DoOne(string file)
        {
            var fileName = Path.GetFileName(file);
            if (r_JQFile.IsMatch(fileName))
            {
                var text = File.ReadAllText(file, Encoding.UTF8);
                var version = r_JQVersion.Match(text)?.Value;
                if (!string.IsNullOrWhiteSpace(version))
                {
                    var item = new JQFileItem 
                    { 
                        Name = Path.GetFileName(file),
                        Path = file, Version = version
                    };
                    try
                    {
                        if (item.Version == ToVersion)
                        {
                            item.Foreground = Brushes.Green;
                            return item;
                        }
                        PathHelper.RemoveAttribute(file, FileAttributes.ReadOnly);
                        if (Backup)
                        {
                            var bak = Path.Combine(_CurBackupFolder, file.Replace(WebRootFolder, string.Empty).TrimStart('\\'));
                            PathHelper.CopyFile(file, bak);
                        }
                        text = r_JQVersion.Replace(text, ToVersion);
                        File.WriteAllText(file, text, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        item.Foreground = Brushes.Red;
                        Logger.Error("JQuery版本替换失败：" + file, ex);
                        MainWindowVM.NotifyWarn($"替换失败：{file}\nv{version}", "JQuery版本替换");
                    }
                    return item;
                }
            }
            return null;
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
                _Find = _Find ?? new RelayCommand<JQFileItem>(_ =>
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
                _FindNext = _FindNext ?? new RelayCommand<JQFileItem>(_ =>
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
        private JQFileItem GetSelectedItem(JQFileItem node = null)
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
        private JQFileItem FindNextItem(JQFileItem item, string findText)
        {
            item = item ?? Data;
            JQFileItem result = null;
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
        private JQFileItem FindParentNext(JQFileItem item, string findText)
        {
            JQFileItem result = null;
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
        private JQFileItem GetParentItem(JQFileItem item, JQFileItem node = null)
        {
            JQFileItem result = null;
            node = node ?? Data;
            foreach (var child in node.Children)
            {
                if (child.Equals(item)) return node;
                result = GetParentItem(item, child);
                if (result != null) return result;
            }
            return null;
        }
        private JQFileItem FindItem(JQFileItem item, string findText)
        {
            JQFileItem result = null;
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
                _OpenLocation = _OpenLocation ?? new RelayCommand<JQFileItem>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (string.IsNullOrEmpty(_?.Path)) return;
                    if (!Directory.Exists(_.Path) && !File.Exists(_.Path))
                    {
                        MainWindowVM.NotifyWarn(_.Path, "路径不存在");
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
                _CopyPath = _CopyPath ?? new RelayCommand<JQFileItem>(_ =>
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
                _CopyName = _CopyName ?? new RelayCommand<JQFileItem>(_ =>
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
            ToVersion = MainWindowVM.ReadSetting($"{nameof(JQueryVerCtrl)}.{nameof(ToVersion)}", ToVersion);
            WebRootFolder = MainWindowVM.ReadSetting($"{nameof(JQueryVerCtrl)}.{nameof(WebRootFolder)}", WebRootFolder);
            Backup = MainWindowVM.ReadSetting($"{nameof(JQueryVerCtrl)}.{nameof(Backup)}", Backup);
            BackupFolder = MainWindowVM.ReadSetting($"{nameof(JQueryVerCtrl)}.{nameof(BackupFolder)}", BackupFolder);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(JQueryVerCtrl)}.{nameof(ToVersion)}", ToVersion);
            MainWindowVM.WriteSetting($"{nameof(JQueryVerCtrl)}.{nameof(WebRootFolder)}", WebRootFolder);
            MainWindowVM.WriteSetting($"{nameof(JQueryVerCtrl)}.{nameof(Backup)}", Backup);
            MainWindowVM.WriteSetting($"{nameof(JQueryVerCtrl)}.{nameof(BackupFolder)}", BackupFolder);
        }
        #endregion
    }
}
