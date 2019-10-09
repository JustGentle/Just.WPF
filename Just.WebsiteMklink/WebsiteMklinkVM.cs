using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Views;
using Microsoft.Web.Administration;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;

namespace Just.WebsiteMklink
{
    [AddINotifyPropertyChangedInterface]
    public class WebsiteMklinkVM
    {
        #region 绑定数据
        public bool Doing { get; set; }
        public string SourceFolder { get; set; }
        public string TargetFolder { get; set; }
        public string Log { get; set; }
        #endregion

        #region 浏览和打开文件夹
        private ICommand _SourceFolderBrowser;
        public ICommand SourceFolderBrowser
        {
            get
            {
                _SourceFolderBrowser = _SourceFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.SourceFolder = dlg.FileName;
                    }
                });
                return _SourceFolderBrowser;
            }
        }
        private ICommand _OpenSourceFolder;
        public ICommand OpenSourceFolder
        {
            get
            {
                _OpenSourceFolder = _OpenSourceFolder ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrEmpty(SourceFolder)) return;
                    System.Diagnostics.Process.Start("explorer.exe", SourceFolder);
                });
                return _OpenSourceFolder;
            }
        }

        private ICommand _TargetFolderBrowser;
        public ICommand TargetFolderBrowser
        {
            get
            {
                _TargetFolderBrowser = _TargetFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.TargetFolder = dlg.FileName;
                    }
                });
                return _TargetFolderBrowser;
            }
        }
        private ICommand _OpenTargetFolder;
        public ICommand OpenTargetFolder
        {
            get
            {
                _OpenTargetFolder = _OpenTargetFolder ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrEmpty(SourceFolder)) return;
                    System.Diagnostics.Process.Start("explorer.exe", TargetFolder);
                });
                return _OpenTargetFolder;
            }
        }
        #endregion

        #region 选择IIS站点
        private ICommand _SourceWebBrowser;
        public ICommand SourceWebBrowser
        {
            get
            {
                _SourceWebBrowser = _SourceWebBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        var dir = GetFolderByWebSite();
                        if (dir == null) return;
                        SourceFolder = dir;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("获取IIS站点错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("获取IIS站点错误"); });
                    }
                });
                return _SourceWebBrowser;
            }
        }
        private ICommand _TargetWebBrowser;
        public ICommand TargetWebBrowser
        {
            get
            {
                _TargetWebBrowser = _TargetWebBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        var dir = GetFolderByWebSite();
                        if (dir == null) return;
                        TargetFolder = dir;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("获取IIS站点错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("获取IIS站点错误"); });
                    }
                });
                return _TargetWebBrowser;
            }
        }

        private string GetFolderByWebSite()
        {
            var sites = GetSiteInfos();
            var items = sites
                .OrderBy(s => s.State == ObjectState.Started ? 0 : 1)
                .Select(s => new Tuple<string, object>($"{s.Name}{(s.State == ObjectState.Started ? string.Empty : "【停止】")}", s));
            var win = new ListWin { Items = new ObservableCollection<Tuple<string, object>>(items) };
            if (win.ShowDialog() != true) return null;
            if (!(win.SelectedItem is SiteInfo site)) return null;
            return site.Dir;
        }
        private IEnumerable<SiteInfo> GetSiteInfos()
        {
            var mgr = new ServerManager();
            return mgr.Sites.Select(s => new SiteInfo
            {
                Id = s.Id,
                Name = s.Name,
                State = s.State,
                Dir = s.Applications.First().VirtualDirectories.First().PhysicalPath
            });
        }
        class SiteInfo
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public ObjectState State { get; set; }
            public string Dir { get; set; }
        }
        #endregion

        #region 映射
        private StringBuilder _logBuilder = new StringBuilder();
        private string[] IgnorePath { get; set; }
        private string[] ActualFiles { get; set; }
        private ICommand _Mklink = null;
        public ICommand Mklink
        {
            get
            {
                _Mklink = _Mklink ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrEmpty(SourceFolder))
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("请先选择来源站点！"); });
                        return;
                    }
                    if (!Directory.Exists(SourceFolder))
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("来源站点目录不存在！"); });
                        return;
                    }
                    if (string.IsNullOrEmpty(TargetFolder))
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("请先选择目标站点！"); });
                        return;
                    }
                    if (Directory.Exists(TargetFolder))
                    {
                        var files = Directory.GetFiles(TargetFolder);
                        var dirs = Directory.GetDirectories(TargetFolder);
                        if (files.Any() || dirs.Any())
                        {
                            var rst = MainWindowVM.DispatcherInvoke(() => { return MessageWin.Confirm("目标站点已存在文件，是否覆盖？"); });
                            if (rst != true)
                                return;
                        }
                    }
                    else
                    {
                        Directory.CreateDirectory(TargetFolder);
                    }
                    _logBuilder = new StringBuilder(Log);
                    Doing = true;
                    MainWindowVM.ShowStatus("映射...");
                    AppendLog("==========映射==========");
                    try
                    {
                        IgnorePath = IgnorePath.EmptyIfNull().ToArray();
                        ActualFiles = ActualFiles.EmptyIfNull().ToArray();

                        var dirs = Directory.GetDirectories(SourceFolder);
                        MainWindowVM.ShowStatus("映射目录...");
                        foreach (var dir in dirs)
                        {
                            MklinkDir(dir);
                        }

                        var files = Directory.GetFiles(SourceFolder);
                        MainWindowVM.ShowStatus("映射文件...");
                        foreach (var file in files)
                        {
                            MklinkFile(file);
                        }

                        AppendLog("==========完成==========");
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Info("映射完成！"); });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("映射过程出现错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("映射过程出现错误！"); });
                    }
                    MainWindowVM.ShowStatus();
                    Doing = false;
                });
                return _Mklink;
            }
        }

        
        private void AppendLog(string msg = null)
        {
            if (!string.IsNullOrEmpty(msg)) _logBuilder.AppendLine(msg);
            MainWindowVM.DispatcherInvoke(() => { Log = _logBuilder.ToString(); });
        }

        private void MklinkDir(string dir)
        {
            var dirName = Path.GetFileName(dir);
            var targetDir = $"{TargetFolder}\\{dirName}";
            _logBuilder.Append(dirName);

            if (IgnorePath.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                _logBuilder.AppendLine(" >>>忽略");
            else
            {
                if (Directory.Exists(targetDir))
                    Directory.Delete(targetDir, false);
                if (CreateSymbolicLink(targetDir, dir, SymbolicLink.Directory))
                    _logBuilder.AppendLine(" >>>映射成功");
                else
                    _logBuilder.AppendLine(" >>>映射失败！！！");
            }

            AppendLog();
        }
        private void MklinkFile(string file)
        {
            var fileName = Path.GetFileName(file);
            var targetFile = $"{TargetFolder}\\{fileName}";
            _logBuilder.Append(fileName);

            if (IgnorePath.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                _logBuilder.AppendLine(" >>>忽略");
            else if (ActualFiles.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    File.Copy(file, targetFile, true);
                    _logBuilder.AppendLine(" >>>复制成功");
                }
                catch (Exception e)
                {
                    _logBuilder.AppendLine(" >>>复制失败！！！");
                    Logger.Error("复制文件错误", e);
                }
            }
            else
            {
                if (File.Exists(targetFile))
                    File.Delete(targetFile);
                if (CreateSymbolicLink(targetFile, file, SymbolicLink.File))
                    _logBuilder.AppendLine(" >>>映射成功");
                else
                    _logBuilder.AppendLine(" >>>映射失败！！！");
            }

            AppendLog();
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);
        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }
        #endregion

        #region Setting
        internal void ReadSetting()
        {
            IgnorePath = MainWindowVM.ReadSetting($"{nameof(WebsiteMklinkCtrl)}.{nameof(IgnorePath)}", IgnorePath);
            ActualFiles = MainWindowVM.ReadSetting($"{nameof(WebsiteMklinkCtrl)}.{nameof(ActualFiles)}", ActualFiles);

            SourceFolder = MainWindowVM.ReadSetting($"{nameof(WebsiteMklinkCtrl)}.{nameof(SourceFolder)}", SourceFolder);
            TargetFolder = MainWindowVM.ReadSetting($"{nameof(WebsiteMklinkCtrl)}.{nameof(TargetFolder)}", TargetFolder);
        }

        internal void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(WebsiteMklinkCtrl)}.{nameof(SourceFolder)}", SourceFolder);
            MainWindowVM.WriteSetting($"{nameof(WebsiteMklinkCtrl)}.{nameof(TargetFolder)}", TargetFolder);
        }
        #endregion
    }
}
