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
        public string WebRootFolder { get; set; }
        public bool Preview { get; set; } = true;
        public bool Backup { get; set; }
        public string BackupFolder { get; set; }
        public int Process { get; set; }
        public ObservableCollection<RevFileItem> Items { get; set; } = new ObservableCollection<RevFileItem>();
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
        private void Scan()
        {
            Status = ActionStatus.Scanning;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Process = 0;
                MainWindow.DispatcherInvoke(Items.Clear);
                while (Process < 100)
                {
                    if (tokenSource.IsCancellationRequested) break;
                    Thread.Sleep(500);
                    MainWindow.DispatcherInvoke(() =>
                    {
                        Items.Add(new RevFileItem
                        {
                            UpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            OrigFile = Process.ToString()
                        });
                    });
                    Process += 10;
                }
                if (Process == 100)
                {
                    Status = ActionStatus.Scanned;
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Info("扫描完成"); });
                    if (!Preview)
                    {
                        ClearAction.Execute(null);
                    }
                }
            }, tokenSource.Token);
        }
        private void Clear()
        {
            Status = ActionStatus.Clearing;
            tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                Process = 0;
                var count = Items.Count();
                while (Items.Any())
                {
                    if (tokenSource.IsCancellationRequested) break;
                    Thread.Sleep(500);
                    MainWindow.DispatcherInvoke(() =>
                    {
                        if (Items.Any())
                            Items.RemoveAt(0);
                    });
                    Process = (int)Math.Floor((count - Items.Count()) * 100m / count);
                }
                if (Process == 100)
                {
                    Status = ActionStatus.Finished;
                    MainWindow.DispatcherInvoke(() => { NotifyWin.Info("清理完成"); });
                }
            }, tokenSource.Token);
        }
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
