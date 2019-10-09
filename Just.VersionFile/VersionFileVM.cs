using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Crypto;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Just.VersionFile
{
    [AddINotifyPropertyChangedInterface]
    public class VersionFileVM
    {
        private const string KeyMainVersion = "Main";
        private const string KeyPatchVersion = "Patch";
        private const string VersionFileName = "Version.ver";
        private VersionFileInfo _versionFile = new VersionFileInfo();
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public bool Doing { get; set; }

        public bool HasCheckData { get; set; }

        public string PackFolder { get; set; }
        public string MainVersion { get; set; } = DateTime.Now.ToString("yyyyMMdd");
        public string MainVersionDescription { get; set; } = DateTime.Now.ToString("yyyy年MM月dd日 完整升级包");

        public bool IsPatch { get; set; }
        public string PatchVersion { get; set; } = DateTime.Now.ToString("yyyyMMdd.HHmm");
        public string PatchVersionDescription { get; set; } = $"范围：所有{Environment.NewLine}内容：功能优化{Environment.NewLine}基于{DateTime.Now:yyyyMMdd}版本";



        private ICommand _PackFolderBrowser;
        public ICommand PackFolderBrowser
        {
            get
            {
                _PackFolderBrowser = _PackFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.PackFolder = dlg.FileName;
                    }
                });
                return _PackFolderBrowser;
            }
        }

        private ICommand _CreateFile;
        public ICommand CreateFile
        {
            get
            {
                _CreateFile = _CreateFile ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (Doing)
                    {
                        _tokenSource.Cancel();
                        return;
                    }

                    _tokenSource = new CancellationTokenSource();
                    Task.Run(() =>
                    {
                        if (string.IsNullOrWhiteSpace(PackFolder))
                        {
                            MainWindowVM.NotifyWarn("升级包路径不能为空");
                            return;
                        }
                        //补丁包基于上一个版本生成
                        if (IsPatch && (_versionFile.Version == null || !_versionFile.Version.ContainsKey(KeyMainVersion)))
                        {
                            if (MainWindowVM.MessageConfirm("未读取上一版本，确定生成补丁？") != true)
                            {
                                return;
                            }
                        }
                        if (string.IsNullOrWhiteSpace(MainVersion) || (IsPatch && string.IsNullOrWhiteSpace(PatchVersion)))
                        {
                            MainWindowVM.NotifyWarn("版本号不能为空");
                            return;
                        }
                        var path = $@"{PackFolder}\{VersionFileName}";
                        if (File.Exists(path))
                        {
                            if (MainWindowVM.MessageConfirm("版本信息文件已存在，是否覆盖？") != true)
                            {
                                return;
                            }
                        }
                        MainWindowVM.ShowStatus("开始生成...");
                        Doing = true;
                        try
                        {
                            var file = new VersionFileInfo
                            {
                                Version = new Dictionary<string, VersionInfo>
                                {
                                    { KeyMainVersion, new VersionInfo{ Name = "主要版本", Version = MainVersion, Description = MainVersionDescription } }
                                }
                            };
                            if (IsPatch)
                            {
                                file.Version.Add(KeyPatchVersion, new VersionInfo { Name = "补丁版本", Version = PatchVersion, Description = PatchVersionDescription });
                            }
                            if (HasCheckData)
                            {
                                MainWindowVM.ShowStatus($"生成文件校验信息...");
                                var checkData = GetAllFileHash();
                                if (checkData == null)
                                {
                                    Doing = false;
                                    MainWindowVM.ShowStatus();
                                    MainWindowVM.NotifyWarn("停止生成");
                                    return;
                                }
                                if (IsPatch)
                                {
                                    file.CheckData = file.CheckData.AddOrUpdate(checkData);
                                }
                                else
                                {
                                    file.CheckData = checkData;
                                }
                            }
                            else
                            {
                                file.CheckData = null;
                            }
                            var json = JsonConvert.SerializeObject(file);
                            var encode = AES.Encrypt(json);
                            File.WriteAllText(path, encode, Encoding.UTF8);
                            _versionFile = file;
                            MainWindowVM.NotifyInfo("生成成功");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("生成版本文件错误", ex);
                            MainWindowVM.NotifyError("生成错误：" + ex.Message);
                        }
                        Doing = false;
                        MainWindowVM.ShowStatus();
                    }, _tokenSource.Token);
                });
                return _CreateFile;
            }
        }
        //不取哈希的文件夹(反斜杠结尾)、文件
        private string[] IgnoreHashs { get; set; }
        private Dictionary<string, string> GetAllFileHash()
        {
            var result = new Dictionary<string, string>();
            //1.取顶层文件
            var files = Directory.GetFiles(PackFolder)
                .Where(f => !IgnoreHashs.Any(i => f.Equals($@"{PackFolder}\{i}", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            //2.取顶层目录
            var dirs = Directory.GetDirectories(PackFolder)
                .Where(d => !IgnoreHashs.Any(i => $"{d}/".Equals($@"{PackFolder}\{i}", StringComparison.OrdinalIgnoreCase)))
                .ToList();
            //3.取子目录文件
            foreach (var dir in dirs)
            {
                if (_tokenSource.IsCancellationRequested) return null;
                files.AddRange(Directory.GetFiles(dir, "*", SearchOption.AllDirectories));
            }
            var fileCount = files.Count;
            var timer = DateTime.Now.Ticks;
            var interval = 1000 * 10000;
            for (int i = 0; i < fileCount; i++)
            {
                if (_tokenSource.IsCancellationRequested) return null;
                try
                {
                    var file = files[i];
                    var key = file.Remove(0, PackFolder.Length + 1).Replace("\\", "/");
                    result.Add(key, MD5.GetFileHash(file));
                    if (DateTime.Now.Ticks - timer > interval)
                    {
                        timer = DateTime.Now.Ticks;
                        MainWindowVM.ShowStatus($"生成文件校验信息...{i}/{fileCount}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("获取文件哈希值错误", ex);
                }
            }
            return result;
        }

        private ICommand _ReadFile;
        public ICommand ReadFile
        {
            get
            {
                _ReadFile = _ReadFile ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        DefaultFileName = VersionFileName
                    };
                    dlg.Filters.Add(new CommonFileDialogFilter("版本文件", "ver"));
                    dlg.Filters.Add(new CommonFileDialogFilter("所有文件", "*"));

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        try
                        {
                            var encode = File.ReadAllText(dlg.FileName, Encoding.UTF8);
                            var json = AES.Decrypt(encode);
                            var file = JsonConvert.DeserializeObject<VersionFileInfo>(json);
                            var baseVersion = string.Empty;
                            if (file.Version.ContainsKey(KeyMainVersion))
                            {
                                MainVersion = file.Version[KeyMainVersion].Version;
                                MainVersionDescription = file.Version[KeyMainVersion].Description;

                                baseVersion = $"基于 {file.Version[KeyMainVersion].Version} 完整包";
                            }
                            if (file.Version.ContainsKey(KeyPatchVersion))
                            {
                                baseVersion = $"基于 {file.Version[KeyPatchVersion].Version} 补丁包";
                            }
                            if (!string.IsNullOrEmpty(baseVersion))
                            {
                                IsPatch = true;
                                var descLines = PatchVersionDescription.Split(Environment.NewLine).ToList();
                                var index = descLines.FindLastIndex(l => l.TrimStart().StartsWith("基于"));
                                if (index >= 0)
                                {
                                    descLines[index] = baseVersion;
                                }
                                else
                                {
                                    descLines.Add(baseVersion);
                                }
                                PatchVersionDescription = string.Join(Environment.NewLine, descLines);
                            }

                            if (file.CheckData != null && file.CheckData.Count > 0)
                            {
                                HasCheckData = true;
                            }
                            else
                            {
                                HasCheckData = false;
                            }
                            _versionFile = file;
                            MainWindowVM.NotifyInfo("读取成功");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("读取版本文件错误", ex);
                            MainWindowVM.NotifyError("读取错误：" + ex.Message);
                        }
                    }
                });
                return _ReadFile;
            }
        }

        public ICommand _ShowFile;
        public ICommand ShowFile
        {
            get
            {
                _ShowFile = _ShowFile ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        var json = JsonConvert.SerializeObject(_versionFile, Formatting.Indented);
                        var n = new MessageWin
                        {
                            Title = $"版本信息({_versionFile?.CheckData?.Count ?? 0})",
                            Message = json,
                            MessageAlignment = HorizontalAlignment.Left,
                            Width = 800
                        };
                        n.ShowDialog();
                    });
                });
                return _ShowFile;
            }
        }

        #region Setting
        public void ReadSetting()
        {
            HasCheckData = MainWindowVM.ReadSetting($"{nameof(VersionFileCtrl)}.{nameof(HasCheckData)}", HasCheckData);
            PackFolder = MainWindowVM.ReadSetting($"{nameof(VersionFileCtrl)}.{nameof(PackFolder)}", PackFolder);
            //MainVersion = MainWindowVM.ReadSetting($"{nameof(VersionFileCtrl)}.{nameof(MainVersion)}", MainVersion);
            //MainVersionDescription = MainWindowVM.ReadSetting($"{nameof(VersionFileCtrl)}.{nameof(MainVersionDescription)}", MainVersionDescription);

            IgnoreHashs = MainWindowVM.ReadSetting($"{nameof(VersionFileCtrl)}.{nameof(IgnoreHashs)}", IgnoreHashs);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(VersionFileCtrl)}.{nameof(HasCheckData)}", HasCheckData);
            MainWindowVM.WriteSetting($"{nameof(VersionFileCtrl)}.{nameof(PackFolder)}", PackFolder);
            //MainWindowVM.WriteSetting($"{nameof(VersionFileCtrl)}.{nameof(MainVersion)}", MainVersion);
            //MainWindowVM.WriteSetting($"{nameof(VersionFileCtrl)}.{nameof(MainVersionDescription)}", MainVersionDescription);
        }
        #endregion
    }
}
