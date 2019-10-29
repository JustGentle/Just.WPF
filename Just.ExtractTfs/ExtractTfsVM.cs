using GenLibrary.MVVM.Base;
using Just.Base;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Just.ExtractTfs
{
    [AddINotifyPropertyChangedInterface]
    public class ExtractTfsVM
    {
        #region 属性
        public bool Doing { get; set; }
        public string CollectionUri { get; set; }
        public string ItemPath { get; set; }
        public string ChangesetIds { get; set; }
        public string SaveFolder { get; set; }
        public bool IsMoveFile { get; set; } = true;
        public bool IsGulpFile { get; set; }
        public bool IsFullChangeset { get; set; }
        #endregion

        #region 方法
        private ICommand _SaveFolderBrowser;
        public ICommand SaveFolderBrowser
        {
            get
            {
                _SaveFolderBrowser = _SaveFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.SaveFolder = dlg.FileName;
                    }
                });
                return _SaveFolderBrowser;
            }
        }
        private ICommand _OpenSaveFolder;
        public ICommand OpenSaveFolder
        {
            get
            {
                _OpenSaveFolder = _OpenSaveFolder ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrEmpty(SaveFolder)) return;
                    System.Diagnostics.Process.Start("explorer.exe", SaveFolder);
                });
                return _OpenSaveFolder;
            }
        }

        private TfsTeamProjectCollection _collection;
        private VersionControlServer _server;
        private ICommand _Extract;
        public ICommand Extract
        {
            get
            {
                _Extract = _Extract ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrEmpty(CollectionUri))
                    {
                        MainWindowVM.NotifyWarn("项目集合路径不能为空");
                        return;
                    }
                    if (string.IsNullOrEmpty(ChangesetIds))
                    {
                        MainWindowVM.NotifyWarn("变更集Id不能为空");
                        return;
                    }
                    if (string.IsNullOrEmpty(SaveFolder) || SaveFolder.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                    {
                        MainWindowVM.NotifyWarn("保存路径无效");
                        return;
                    }
                    Task.Run(() =>
                    {
                        Doing = true;
                        MainWindowVM.ShowStatus("提取...");
                        List<int> changesetIdList;
                        try
                        {
                            MainWindowVM.ShowStatus("读取变更集Id...");
                            changesetIdList = GetChangesetIdList();

                            MainWindowVM.ShowStatus("连接项目集合...");
                            ConnectServer();

                            MainWindowVM.ShowStatus("查找变更集...");
                            changesetIdList = FilterChangesetIdList(changesetIdList);
                            if (!changesetIdList.Any())
                            {
                                MainWindowVM.NotifyWarn("没有可以提取的变更集");
                                return;
                            }

                            MainWindowVM.ShowStatus("查找变更集文件...");
                            var items = GetChangeItems(changesetIdList);
                            if (!items.Any())
                            {
                                MainWindowVM.NotifyWarn("没有可以提取的文件");
                                return;
                            }

                            MainWindowVM.ShowStatus("提取变更集文件...", true);
                            var folder = Path.Combine(SaveFolder, DateTime.Now.ToString("yyyy-MM-dd.HHmmss.fff"));
                            DownLoadItems(folder, items);
                            if(IsGulpFile) Gulp(folder);

                            if (MainWindowVM.MessageConfirm("提取完成，是否打开") != true) return;
                            System.Diagnostics.Process.Start("explorer.exe", folder);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("提取过程出现错误", ex);
                            MainWindowVM.NotifyError("提取过程出现错误：" + ex.Message);
                        }
                        finally
                        {
                            Doing = false;
                            MainWindowVM.ShowStatus();
                        }
                    });
                });
                return _Extract;
            }
        }
        private List<int> GetChangesetIdList()
        {
            var changesetIdList = new List<int>();
            var parts = ChangesetIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var result = parts.SelectMany(part =>
            {
                var values = new List<int>();
                if (part.Contains('-'))
                {
                    var limits = part.Split(new[] { '-' }, 2);
                    if (limits.Length != 2)
                    {
                        throw new Exception("变更集无效：" + part);
                    }
                    if (!int.TryParse(limits[0], out int min))
                    {
                        throw new Exception("变更集无效：" + part);
                    }
                    if (!int.TryParse(limits[1], out int max))
                    {
                        throw new Exception("变更集无效：" + part);
                    }
                    //互换
                    if (min > max)
                    {
                        min = min + max;
                        max = min - max;
                        min = min - max;
                    }
                    for (int i = min; i <= max; i++)
                    {
                        values.Add(i);
                    }
                    return values;
                }
                else
                {
                    if (!int.TryParse(part, out int value))
                    {
                        throw new Exception("变更集无效：" + part);
                    }
                    values.Add(value);
                }
                return values;
            });
            //去重
            return result.Distinct().ToList();
        }
        private void ConnectServer()
        {
            try
            {
                _collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(CollectionUri));
                _server = _collection.GetService<VersionControlServer>();
            }
            catch (Exception ex)
            {
                throw new Exception("连接项目集合失败", ex);
            }
        }
        private List<int> FilterChangesetIdList(List<int> changesetIdList)
        {
            var his = _server.QueryHistory(new QueryHistoryParameters(ItemPath, RecursionType.Full));
            changesetIdList = his
                .Where(h => changesetIdList.Contains(h.ChangesetId))
                                    .Select(h => h.ChangesetId)
                                    .OrderBy(id => id)
                                    .ToList();
            return changesetIdList;
        }
        private Dictionary<string, Item> GetChangeItems(List<int> changesetIdList)
        {
            var count = changesetIdList.Count();
            var index = 0;
            var items = new Dictionary<string, Item>();
            foreach (var changesetId in changesetIdList)
            {
                var changeset = _server.GetChangeset(changesetId, true, false, true);
                var changes = changeset.Changes
                    .Where(change => change.Item.ItemType == ItemType.File && change.ChangeType != ChangeType.None);
                foreach (var change in changes)
                {
                    var serverItem = change.Item.ServerItem;
                    if (!IsFullChangeset && !serverItem.Equals(ItemPath) && !serverItem.Contains(ItemPath + "/")) continue; //过滤
                    if (change.ChangeType.HasFlag(ChangeType.Delete) && items.ContainsKey(serverItem))
                    {
                        //移除文件
                        items.Remove(serverItem);
                    }
                    else
                    {
                        //覆盖之前变更集
                        items.AddOrUpdate(serverItem, change.Item);
                    }
                }
                index++;
                MainWindowVM.ShowStatus(null, true, index * 20 / count);//20%
            }
            return items;
        }
        private void DownLoadItems(string folder, Dictionary<string, Item> items)
        {
            if (IsMoveFile) InitFileRegex();
            MainWindowVM.BeginStatusInterval();
            var count = items.Count;
            var index = 0;
            Parallel.ForEach(items, item =>
            {
                var path = Path.Combine(folder, GetItemFileName(folder, item.Value));
                DownloadItem(item.Value, path);
                index++;
                MainWindowVM.ShowStatus($"提取变更集文件...{index}/{count}", true, 20 + index * 80 / count);//80%
            });
            MainWindowVM.EndStatusInterval();
            MainWindowVM.ShowStatus($"提取变更集文件...{count}/{count}", true, 100);//50%
        }
        private Dictionary<string, string> _FileRegex { get; set; }
        private Dictionary<string, Regex> _fileRegex = new Dictionary<string, Regex>();
        private void InitFileRegex()
        {
            _fileRegex = new Dictionary<string, Regex>();
            if (_FileRegex == null) return;
            foreach (var kv in _FileRegex)
            {
                _fileRegex.Add(kv.Value, new Regex(kv.Key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }
        private string GetItemFileName(string folder, Item item)
        {
            var file = item.ServerItem.TrimStart('$', '/').Replace("/", "\\");
            if (!IsMoveFile) return file;
            foreach (var kv in _fileRegex)
            {
                if (kv.Value.IsMatch(file))
                {
                    file = kv.Value.Replace(file, kv.Key);
                    return file;
                }
            }
            return file;
        }
        private void DownloadItem(Item item, string path)
        {
            item.DownloadFile(path);
        }
        private void Gulp(string folder)
        {
            var outputs = Directory.GetDirectories(folder);
            var path = $@"{AppDomain.CurrentDomain.BaseDirectory}\gulp";
            var topFiles = Directory.GetFiles(path);
            var topFolders = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var output in outputs)
            {
                var console = string.Empty;
                MainWindowVM.ShowStatus("gulp...");
                foreach (var file in files)
                {
                    var copy = Path.Combine(output, file.Remove(0, path.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                    File.Copy(file, copy);
                }
                var info = new ProcessStartInfo
                {
                    FileName = "cmd",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = output,
                    CreateNoWindow = true
                };
                var cmd = Process.Start(info);
                cmd.StandardInput.WriteLine("cnpm install");
                cmd.StandardInput.WriteLine("gulp changed");
                cmd.StandardInput.WriteLine("exit");
                console += cmd.StandardOutput.ReadToEnd() + cmd.StandardError.ReadToEnd();
                cmd.WaitForExit();
                cmd.Close();
                File.WriteAllText(Path.Combine(output, "gulp.log"), console, Encoding.UTF8);
                foreach (var topFile in topFiles)
                {
                    var copy = Path.Combine(output, topFile.Remove(0, path.Length + 1));
                    File.Delete(copy);
                }
            }
        }
        #endregion

        #region Setting
        public void ReadSetting()
        {
            CollectionUri = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(CollectionUri)}", CollectionUri);
            ItemPath = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(ItemPath)}", ItemPath);
            ChangesetIds = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(ChangesetIds)}", ChangesetIds);
            SaveFolder = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(SaveFolder)}", SaveFolder);
            IsMoveFile = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(IsMoveFile)}", IsMoveFile);
            _FileRegex = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(_FileRegex)}", _FileRegex);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(CollectionUri)}", CollectionUri);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(ItemPath)}", ItemPath);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(ChangesetIds)}", ChangesetIds);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(SaveFolder)}", SaveFolder);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(IsMoveFile)}", IsMoveFile);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(_FileRegex)}", _FileRegex);
        }
        #endregion
    }
}
