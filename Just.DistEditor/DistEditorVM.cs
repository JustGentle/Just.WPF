using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Crypto;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;

namespace Just.DistEditor
{
    [AddINotifyPropertyChangedInterface]
    public class DistEditorVM
    {
        public bool Doing { get; set; }
        public string FilePath { get; set; }


        private const string FileSeparator = "|";
        private ICommand _FileBrowser;
        public ICommand FileBrowser
        {
            get
            {
                _FileBrowser = _FileBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        Multiselect = false//true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.FilePath = string.Join(FileSeparator, dlg.FileNames);
                        this.UpdateFile.Execute(null);
                    }
                });
                return _FileBrowser;
            }
        }

        private ICommand _FolderBrowser;
        public ICommand FolderBrowser
        {
            get
            {
                _FolderBrowser = _FolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.FilePath = dlg.FileName;
                        this.UpdateFile.Execute(null);
                    }
                });
                return _FolderBrowser;
            }
        }

        private ICommand _UpdateFile;
        public ICommand UpdateFile
        {
            get
            {
                _UpdateFile = _UpdateFile ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        if (string.IsNullOrWhiteSpace(FilePath))
                        {
                            MainWindowVM.NotifyWarn("文件路径不能为空");
                            return;
                        }
                        MainWindowVM.ShowStatus("开始更新...");
                        Doing = true;
                        try
                        {
                            var files = GetFiles();
                            if (!files.Any())
                            {
                                Doing = false;
                                MainWindowVM.ShowStatus();
                                MainWindowVM.MessageInfo("没有可以更新的文件");
                                return;
                            }

                            //读取rev
                            MainWindowVM.ShowStatus("读取rev...");
                            var folder = Path.GetDirectoryName(files.First()) + "\\";
                            var distFolder = folder;
                            distFolder = folder.Substring(0, folder.IndexOf(@"\dist\") + @"\dist\".Length);
                            var homeFilename = Path.Combine(distFolder, @"..\Views\dist\Home\index.cshtml");
                            if (!File.Exists(homeFilename))
                            {
                                Doing = false;
                                MainWindowVM.ShowStatus();
                                MainWindowVM.MessageInfo("主页文件不存在");
                                return;
                            }
                            var homeText = File.ReadAllText(homeFilename, Encoding.UTF8);
                            var revFilename = Regex.Match(homeText, @"\brev-manifest-[0-9a-f]{10}\.json\b").Value;
                            revFilename = Path.Combine(distFolder, revFilename);
                            if (!File.Exists(revFilename))
                            {
                                Doing = false;
                                MainWindowVM.ShowStatus();
                                MainWindowVM.MessageInfo("映射文件不存在：" + revFilename);
                                return;
                            }

                            //文件hash
                            MainWindowVM.ShowStatus("生成哈希值...");
                            var rev = new Dictionary<string, string>();
                            var renameReg = new Regex(@"-[0-9a-f]{10}\.");
                            foreach (var file in files)
                            {
                                if (!File.Exists(file))
                                {
                                    Logger.Warn("Dist文件不存在：" + file);
                                    continue;
                                }
                                //跳过映射文件
                                var filename = Path.GetFileName(file);
                                if (filename == "rev-manifest.json" || filename.StartsWith("rev-manifest-"))
                                {
                                    continue;
                                }

                                folder = Path.GetDirectoryName(file);

                                //生成新文件hash
                                var hash = MD5.GetFileHash(file).Replace("-", "").ToLower().Substring(0, 10);
                                var newname = filename;
                                if (renameReg.IsMatch(filename))
                                {
                                    //替换hash
                                    newname = renameReg.Replace(filename, "-" + hash + ".");
                                }
                                else
                                {
                                    Logger.Warn("Dist文件名无哈希值：" + file);
                                    continue;
                                    ////添加hash，按gulp规则，直接截取第一个点.
                                    //var index = filename.IndexOf('.');
                                    //if (index == -1)
                                    //    rename = filename + hash;
                                    //else
                                    //    rename = filename.Substring(0, index) + "-" + hash + filename.Substring(index);
                                }

                                //无更改
                                if (filename == newname)
                                    continue;

                                //rev
                                newname = Path.Combine(folder, newname);
                                rev.Add(file, newname);
                            }

                            //文件替换
                            MainWindowVM.ShowStatus("文件替换...");
                            if (!rev.Any())
                            {
                                Doing = false;
                                MainWindowVM.ShowStatus();
                                MainWindowVM.MessageInfo("没有需要更新的文件");
                                return;
                            }
                            var revText = File.ReadAllText(revFilename, Encoding.UTF8);
                            foreach (var kv in rev)
                            {
                                var filename = kv.Key;
                                var newname = kv.Value;
                                File.Copy(filename, newname, true);
                                filename = filename.Replace(distFolder, string.Empty).Replace("\\", "/");
                                newname = newname.Replace(distFolder, string.Empty).Replace("\\", "/");
                                revText = revText.Replace($"\"{filename}\"", $"\"{newname}\"");
                            }

                            //保存rev
                            MainWindowVM.ShowStatus("保存映射文件...");
                            var revHash = MD5.GetTextHash(revText).Replace("-", "").ToLower().Substring(0, 10);
                            var revNewname = renameReg.Replace(revFilename, "-" + revHash + ".");
                            File.WriteAllText(revNewname, revText, Encoding.UTF8);

                            //更新Index
                            MainWindowVM.ShowStatus("更新主页...");
                            rev.Add(revFilename, revNewname);
                            rev = rev.Where(kv =>
                            kv.Key.Contains(@"\dist\rev-manifest-")
                            || kv.Key.Contains(@"\dist\app\common\style\startloader-")
                            || kv.Key.Contains(@"\dist\app\sys_const-")
                            || kv.Key.Contains(@"\dist\scripts\require-")
                            || kv.Key.Contains(@"\dist\app\loadRevManifest-")
                            || kv.Key.Contains(@"\dist\app\main-")).ToDictionary(kv => Path.GetFileName(kv.Key), kv => Path.GetFileName(kv.Value));

                            ReplaceIndexText(homeFilename, rev);
                            ReplaceIndexText(Path.Combine(distFolder, @"..\Views\dist\Sub\index.cshtml"), rev);
                            ReplaceIndexText(Path.Combine(distFolder, @"..\Views\dist\Public\index.cshtml"), rev);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("文件更新错误", ex);
                            MainWindowVM.NotifyError("更新错误：" + ex.Message);
                        }
                        Doing = false;
                        MainWindowVM.ShowStatus();
                        MainWindowVM.MessageInfo("更新完成");
                    });
                });
                return _UpdateFile;
            }
        }
        private IEnumerable<string> GetFiles()
        {
            IEnumerable<string> files;
            if (FilePath.Contains(FileSeparator) || File.Exists(FilePath))
            {
                files = FilePath.Split(FileSeparator);
            } 
            else if (Directory.Exists(FilePath))
            {
                files = Directory.GetFiles(FilePath, "*", SearchOption.AllDirectories);
            }
            else
            {
                files = Enumerable.Empty<string>();
            }
            return files;
        }
        private void ReplaceIndexText(string filename, Dictionary<string, string> replacements)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            var text = File.ReadAllText(filename, Encoding.UTF8);
            foreach (var kv in replacements)
            {
                text = text.Replace(kv.Key, kv.Value);
            }
            File.WriteAllText(filename, text, Encoding.UTF8);
        }


        #region Setting
        public void ReadSetting()
        {
            FilePath = MainWindowVM.ReadSetting($"{nameof(DistEditorCtrl)}.{nameof(FilePath)}", FilePath);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(DistEditorCtrl)}.{nameof(FilePath)}", FilePath);
        }
        #endregion
    }
}
