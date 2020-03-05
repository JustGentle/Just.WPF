using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Utils;
using Just.Base.Views;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public string RevFile { get; set; }
        public string BinFolder { get; set; }
        public bool IsMoveFile { get; set; } = true;
        public bool IsGulpFile { get; set; }
        public bool IsFullChangeset { get; set; }
        public int? ItemPathTreeHeight { get; set; } = 0;

        public SourceItemNode ItemPathTree { get; set; }
        #endregion

        #region 选择
        private string _cacheHisItemPath = string.Empty;
        private IEnumerable<Tuple<string, object>> _cacheHis = Enumerable.Empty<Tuple<string, object>>();
        private ICommand _ChangesetBrowser;
        public ICommand ChangesetBrowser
        {
            get
            {
                _ChangesetBrowser = _ChangesetBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (!_Connected || _cacheHisItemPath != ItemPath)
                            {
                                MainWindowVM.ShowStatus("连接项目集合...");
                                if (!ConnectServer()) return;
                                MainWindowVM.ShowStatus("获取变更集...");
                                var his = _server.QueryHistory(new QueryHistoryParameters(ItemPath, RecursionType.Full) { IncludeChanges = false, IncludeDownloadInfo = false });
                                _cacheHis = his.Select(h => new Tuple<string, object>($"{h.ChangesetId} ({h.CreationDate:yyyy-MM-dd HH:mm}) - {h.Owner} - {h.Comment.OneLine()}", h)).ToList();
                                _cacheHisItemPath = ItemPath;
                            }
                            MainWindowVM.ShowStatus("选择...");
                            MainWindowVM.DispatcherInvoke(() =>
                            {
                                var selector = new ListWin
                                {
                                    MultiSelect = true,
                                    Width = 1024,
                                    Items = _cacheHis
                                };
                                if (selector.ShowDialog() != true) return;
                                ChangesetIds = GetChangesetIds(selector.SelectedItems.Cast<Changeset>());
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("获取变更集失败", ex);
                            MainWindowVM.NotifyError("获取变更集失败：" + ex.Message);
                        }
                        finally
                        {
                            MainWindowVM.ShowStatus();
                        }
                    });
                });
                return _ChangesetBrowser;
            }
        }
        private string GetChangesetIds(IEnumerable<Changeset> changesets)
        {
            var idList = changesets.Select(c => c.ChangesetId).OrderBy(i => i).ToList();
            var ids = new StringBuilder();
            int min = -1, max = -1;
            for (int i = 0; i < idList.Count; i++)
            {
                var id = idList[i];
                //不连续,或最后一个则添加
                if (id - max != 1 || i + 1 == idList.Count)
                {
                    //最后一个为连续值,则并入连续值
                    if (id - max == 1) max = id;
                    //连续多个
                    if (min != max)
                    {
                        ids.Append("-").Append(max);
                    }
                    //单值
                    if (id != max)
                    {
                        if (i != 0) ids.Append(",");
                        ids.Append(id);
                    }
                    min = id;
                }
                //否则并入连续值
                max = id;
            }
            return ids.ToString();
        }

        private ICommand _ItemPathBrowser;
        public ICommand ItemPathBrowser
        {
            get
            {
                _ItemPathBrowser = _ItemPathBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        ItemPathTreeHeight = ItemPathTreeHeight.HasValue ? null as int? : 0;
                        if (ItemPathTreeHeight.HasValue) return;
                        if (!ConnectServer()) return;

                        if (ItemPathTree == null) InitItemPathTree();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("获取源码结构错误", ex);
                        MainWindowVM.NotifyError("获取源码结构错误:" + ex.Message);
                    }
                });
                return _ItemPathBrowser;
            }
        }
        private void InitItemPathTree()
        {
            MainWindowVM.ShowStatus("加载项目路径节点...");
            ItemPathTree = new SourceItemNode(null);
            var root = new SourceItemNode(new Item(_server, ItemType.Folder, "$/")) { IsExpanded = true };
            ItemPathTree.Children.Add(root);
            LoadChildrenNode(root, ItemPath);
            MainWindowVM.ShowStatus();
        }
        private void LoadChildrenNode(SourceItemNode node, string toPath = null)
        {
            if (node.IsFile) return;
            node.Children.Clear();
            var itemSet = _server.GetItems(node.SourceItem.ServerItem + "/*");
            var nodes = itemSet.Items.Select(i => new SourceItemNode(i));
            foreach (var item in nodes)
            {
                if (toPath?.StartsWith(item.ServerPath + "/") ?? false)
                {
                    item.IsExpanded = true;
                    LoadChildrenNode(item, toPath);
                }
                else if(item.ServerPath.Equals(toPath))
                {
                    item.IsSelected = true;
                }
                node.Children.Add(item);
            }
        }
        private ICommand _ItemPathSelected;
        public ICommand ItemPathSelected
        {
            get
            {
                _ItemPathSelected = _ItemPathSelected ?? new RelayCommand<GenLibrary.GenControls.TreeListView>(_ =>
                {
                    if (!(_.SelectedItem is SourceItemNode node)) return;
                    ItemPath = node.SourceItem.ServerItem;
                });
                return _ItemPathSelected;
            }
        }
        private ICommand _ItemPathDoubleClick;
        public ICommand ItemPathDoubleClick
        {
            get
            {
                _ItemPathDoubleClick = _ItemPathDoubleClick ?? new RelayCommand<GenLibrary.GenControls.TreeListView>(_ =>
                {
                    if (!(_.SelectedItem is SourceItemNode node)) return;
                    if (node.Children.Any())
                    {
                        node.IsExpanded = !node.IsExpanded;
                        return;
                    }
                    LoadChildrenNode(node);
                });
                return _ItemPathDoubleClick;
            }
        }
        #endregion

        #region 保存路径
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
        #endregion

        #region 映射文件
        private ICommand _RevFileBrowser;
        public ICommand RevFileBrowser
        {
            get
            {
                _RevFileBrowser = _RevFileBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog();
                    dlg.Filters.Add(new CommonFileDialogFilter("映射文件", "*.json"));
                    dlg.Filters.Add(new CommonFileDialogFilter("所有文件", "*"));

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.RevFile = dlg.FileName;
                    }
                });
                return _RevFileBrowser;
            }
        }
        private ICommand _BinFolderBrowser;
        public ICommand BinFolderBrowser
        {
            get
            {
                _BinFolderBrowser = _BinFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true,
                        InitialDirectory = Dialog.GetInitialDirectory(BinFolder)
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.BinFolder = dlg.FileName;
                    }
                });
                return _BinFolderBrowser;
            }
        }
        #endregion

        #region 提取
        private TfsTeamProjectCollection _collection;
        private VersionControlServer _server;
        private ICommand _Extract;
        public ICommand Extract
        {
            get
            {
                _Extract = _Extract ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
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
                            if (!ConnectServer()) return;

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
                            if (IsGulpFile) Gulp(folder);

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

        private bool _Connected => _collection?.Uri.AbsoluteUri.Equals(CollectionUri, StringComparison.OrdinalIgnoreCase) ?? false;
        private bool ConnectServer()
        {
            if (string.IsNullOrEmpty(CollectionUri))
            {
                MainWindowVM.NotifyWarn("项目集合路径不能为空");
                return false;
            }
            try
            {
                if (_Connected) return true;
                _collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(CollectionUri));
                _server = _collection.GetService<VersionControlServer>();
                return true;
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

        private List<string> _roots = new List<string>();
        private string _RootRegex { get; set; }
        private Regex _rootRegex;
        private Dictionary<string, string> _FileRegex { get; set; }
        private Dictionary<string, Regex> _fileRegex = new Dictionary<string, Regex>();
        private void InitFileRegex()
        {
            _roots = new List<string>();
            _rootRegex = new Regex(_RootRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _fileRegex = new Dictionary<string, Regex>();
            if (_FileRegex == null) return;
            foreach (var kv in _FileRegex)
            {
                _fileRegex.Add(kv.Value, new Regex(kv.Key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }
        }
        private string GetItemFileName(string folder, Item item)
        {
            //TODO: 项目文件夹不一定是项目实际名称
            var file = item.ServerItem.TrimStart('$', '/').Replace("/", "\\");
            if (!IsMoveFile) return file;
            var path = _rootRegex.Match(file).Value;
            if (!string.IsNullOrEmpty(path) && !_roots.Contains(path, StringComparer.OrdinalIgnoreCase)) _roots.Add(path);
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
            //Bin
            if (Path.GetDirectoryName(path).EndsWith("\\bin", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(path)) return;
                var source = Path.Combine(BinFolder, Path.GetFileName(path));
                if (File.Exists(source))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    try
                    {
                        File.Copy(source, path);
                    }
                    catch (IOException ex)
                    {
                        if(ex.HResult != -2147024816 && ex.HResult != -2147024713)//文件已存在
                        {
                            throw;
                        }
                    }
                    return;
                }
            }
            item.DownloadFile(path);
        }

        private void Gulp(string folder)
        {
            try
            {
                var outputs = _roots.Select(r => Path.Combine(folder, r));
                if (!outputs.Any()) return;
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gulp");
                foreach (var output in outputs)
                {
                    var console = string.Empty;
                    MainWindowVM.ShowStatus("gulp...");

                    //rev
                    var revDist = Path.Combine(output, "dist", "rev-manifest.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(revDist));
                    File.Copy(RevFile, revDist, true);

                    //gulp
                    PathHelper.CopyDirectory(path, output);
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
                    //var node_modules = Path.Combine(output, "node_modules");
                    //if (Directory.Exists(node_modules))
                    //{
                    //    cmd = Process.Start(info);
                    //    cmd.StandardInput.WriteLine("rmdir /s/q node_modules");
                    //    cmd.StandardInput.WriteLine("exit");
                    //    console += cmd.StandardOutput.ReadToEnd() + cmd.StandardError.ReadToEnd();
                    //    cmd.WaitForExit();
                    //    cmd.Close();
                    //    File.WriteAllText(Path.Combine(output, "gulp.log"), console, Encoding.UTF8);
                    //}
                }
            }
            catch (Exception ex)
            {
                Logger.Error("编译前端资源出现错误", ex);
                MainWindowVM.NotifyError("编译前端资源出现错误：" + ex.Message);
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
            RevFile = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(RevFile)}", RevFile);
            BinFolder = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(BinFolder)}", BinFolder);
            IsMoveFile = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(IsMoveFile)}", IsMoveFile);
            IsGulpFile = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(IsGulpFile)}", IsGulpFile);
            _FileRegex = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(_FileRegex)}", _FileRegex);
            _RootRegex = MainWindowVM.ReadSetting($"{nameof(ExtractTfsCtrl)}.{nameof(_RootRegex)}", _RootRegex);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(CollectionUri)}", CollectionUri);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(ItemPath)}", ItemPath);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(ChangesetIds)}", ChangesetIds);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(SaveFolder)}", SaveFolder);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(RevFile)}", RevFile);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(BinFolder)}", BinFolder);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(IsMoveFile)}", IsMoveFile);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(IsGulpFile)}", IsGulpFile);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(_FileRegex)}", _FileRegex);
            MainWindowVM.WriteSetting($"{nameof(ExtractTfsCtrl)}.{nameof(_RootRegex)}", _RootRegex);
        }
        #endregion
    }
}
