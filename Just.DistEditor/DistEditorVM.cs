using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GenLibrary.MVVM.Base;
using ICSharpCode.AvalonEdit.Highlighting;
using Just.Base;
using Just.Base.Crypto;
using Just.Base.Utils;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;

namespace Just.DistEditor
{
    [AddINotifyPropertyChangedInterface]
    public class DistEditorVM
    {
        #region 属性
        public bool Doing { get; set; }
        public string InputWebsitePath { get; set; }//输入的目录
        public string WebsitePath { get; set; }//已读取的目录
        public RevNodeItem RevFileTree { get; set; }
        #endregion

        #region 选择目录
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
                        this.InputWebsitePath = dlg.FileName;
                        LoadRev.Execute(null);
                    }
                });
                return _FolderBrowser;
            }
        }
        #endregion

        #region 加载Rev
        private ICommand _LoadRev;
        public ICommand LoadRev
        {
            get
            {
                _LoadRev = _LoadRev ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        //目录
                        if (string.IsNullOrEmpty(InputWebsitePath))
                        {
                            MainWindowVM.NotifyWarn("请选择站点目录");
                            return;
                        }
                        else if (!Directory.Exists(InputWebsitePath))
                        {
                            MainWindowVM.NotifyWarn("站点目录不存在：" + InputWebsitePath);
                            return;
                        }

                        MainWindowVM.ShowStatus("开始加载...");
                        Doing = true;
                        try
                        {
                            //rev
                            var revFileName = GetRevFileName(InputWebsitePath);
                            if (string.IsNullOrEmpty(revFileName))
                            {
                                MainWindowVM.NotifyWarn("读取映射文件失败");
                                return;
                            }
                            revFileName = Path.Combine(InputWebsitePath, "dist", revFileName);
                            if (!File.Exists(revFileName))
                            {
                                MainWindowVM.NotifyWarn("映射文件不存在：" + revFileName);
                                return;
                            }

                            this.WebsitePath = this.InputWebsitePath;
                            MainWindowVM.DispatcherInvoke(() => LoadRevNode(revFileName));
                            MainWindowVM.NotifyInfo("加载完成");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("加载错误", ex);
                            MainWindowVM.NotifyError("加载错误：" + ex.Message);
                        }
                        finally
                        {
                            Doing = false;
                            MainWindowVM.ShowStatus();
                        }
                    });
                });
                return _LoadRev;
            }
        }
        private string GetRevFileName(string website)
        {
            var homeFileName = Path.Combine(website, @"Views\dist\Home\index.cshtml");
            if (!File.Exists(homeFileName))
            {
                return string.Empty;
            }
            var homeText = File.ReadAllText(homeFileName, Encoding.UTF8);
            var revFileName = Regex.Match(homeText, @"\brev-manifest-[0-9a-f]{10}\.json\b").Value;
            return revFileName;
        }
        private RevNodeItem LoadRevNode(string file)
        {
            var text = File.ReadAllText(file, Encoding.UTF8);
            var json = JsonConvert.DeserializeObject<JObject>(text);

            RevFileTree = new RevNodeItem() { Path = file };
            LoadRevNode(json, RevFileTree);
            return RevFileTree;
        }
        private RevNodeItem LoadRevNode(JToken token, RevNodeItem parent)
        {
            var node = new RevNodeItem();
            if (token.Type == JTokenType.Object)
            {
                if (parent == RevFileTree)
                {
                    node.IsExpanded = true;
                    node.Path = Path.Combine(WebsitePath, "dist");
                    node.Name = "dist";
                    parent.Children.Add(node);
                }
                else
                {
                    node = parent;
                }
                foreach (var p in (token as JObject).Properties())
                {
                    LoadRevNode(p, node);
                }
            }
            else if (token.Type == JTokenType.Property)
            {
                var p = token as JProperty;
                var v = p.Value;
                node.Path = Path.Combine(parent.Path, p.Name);
                node.Name = p.Name;
                parent.Children.Add(node);
                LoadRevNode(v, node);
            }
            else
            {
                parent.IsFile = true;
                node.Path = Path.Combine(WebsitePath, "dist", token.ToString().Replace("/", "\\"));
                node.Name = Path.GetFileName(node.Path);
                node.IsFile = true;
                parent.Children.Add(node);
            }
            return node;
        }
        #endregion

        #region 打开文件
        private ICommand _ItemPathDoubleClick;
        public ICommand ItemPathDoubleClick
        {
            get
            {
                _ItemPathDoubleClick = _ItemPathDoubleClick ?? new RelayCommand<GenLibrary.GenControls.TreeListView>(_ =>
                {
                    if (!(_.SelectedItem is RevNodeItem node))
                        return;
                    if (node.Children?.Any() == true)
                        return;

                    if (!File.Exists(node.Path))
                    {
                        MainWindowVM.NotifyWarn("文件不存在：" + node.Path);
                        return;
                    }
                    var text = File.ReadAllText(node.Path, Encoding.UTF8);

                    var n = new MessageWin
                    {
                        Title = node.Name,
                        InputValue = text,
                        OkContent = "更新",
                        CancelContent = "取消",
                        Syntax = MainWindowVM.GetFileSyntax(node.Path),
                        IsEditor = true,
                        IsInput = true,
                        IsConfirm = true,
                        MessageAlignment = HorizontalAlignment.Left,
                        Width = MainWindowVM.Instance.MainWindow.Width,
                        EditorMinHeight = MainWindowVM.Instance.MainWindow.Height,
                        EditorShowLineNumbers = true,
                        EditorWrap = true,
                        Owner = MainWindowVM.Instance.MainWindow
                    };
                    if (n.ShowDialog() == true)
                    {
                        UpdateFile.Execute(new Tuple<RevNodeItem, string>(node, n.InputValue));
                    }
                });
                return _ItemPathDoubleClick;
            }
        }
        #endregion

        #region 更新文件
        private ICommand _UpdateFile;
        public ICommand UpdateFile
        {
            get
            {
                _UpdateFile = _UpdateFile ?? new RelayCommand<Tuple<RevNodeItem, string>>(_ =>
                {
                    Task.Run(() =>
                    {
                        if (string.IsNullOrWhiteSpace(WebsitePath))
                        {
                            MainWindowVM.NotifyWarn("站点目录不能为空");
                            return;
                        }
                        MainWindowVM.ShowStatus("开始更新...");
                        Doing = true;
                        try
                        {
                            //数据检查
                            var text = _.Item2;
                            var node = _.Item1;
                            if (node == null)
                            {
                                MainWindowVM.MessageWarn("没有可以更新的数据");
                                return;
                            }
                            var file = node.Path;
                            if (!File.Exists(file))
                            {
                                MainWindowVM.MessageWarn("文件不存在：" + file);
                                return;
                            }
                            var filename = Path.GetFileName(file);
                            if (filename == "rev-manifest.json" || filename.StartsWith("rev-manifest-"))
                            {
                                MainWindowVM.MessageWarn("映射文件请勿直接修改");
                                return;
                            }
                            var revFile = RevFileTree.Path;
                            if (!File.Exists(revFile))
                            {
                                MainWindowVM.MessageWarn("映射文件不存在：" + revFile);
                                return;
                            }

                            //生成新文件hash
                            MainWindowVM.ShowStatus("生成哈希值...");
                            var renameReg = new Regex(@"-[0-9a-f]{10}\.");
                            var hash = MD5.GetTextHash(text).Replace("-", "").ToLower().Substring(0, 10);
                            var newname = filename;
                            if (renameReg.IsMatch(filename))
                            {
                                //替换hash
                                newname = renameReg.Replace(filename, "-" + hash + ".");
                            }
                            else
                            {
                                MainWindowVM.MessageWarn("文件名无哈希值：" + filename);
                                return;
                                ////添加hash，按gulp规则，直接截取第一个点.
                                //var index = filename.IndexOf('.');
                                //if (index == -1)
                                //    rename = filename + hash;
                                //else
                                //    rename = filename.Substring(0, index) + "-" + hash + filename.Substring(index);
                            }

                            //无更改
                            if (filename == newname)
                            {
                                MainWindowVM.MessageWarn("文件无更改");
                                return;
                            }

                            //文件更新
                            MainWindowVM.ShowStatus("文件更新...");
                            var folder = Path.GetDirectoryName(file);
                            var newfile = Path.Combine(folder, newname);
                            var updates = new Dictionary<string, string>
                            {
                                { file, newfile },
                            };

                            //更新rev内容
                            var revText = File.ReadAllText(revFile, Encoding.UTF8);
                            var revJson = JsonConvert.DeserializeObject<JObject>(revText);

                            var value = GetRevToken(revJson, file);
                            if(value == null)
                            {
                                MainWindowVM.MessageWarn("映射中找不到文件：" + filename);
                                return;
                            }
                            File.WriteAllText(newfile, text, Encoding.UTF8);
                            //更新文件节点
                            node.Path = newfile;
                            node.Name = newname;
                            var distFolder = Path.Combine(WebsitePath, "dist") + "\\";
                            value.Value = newfile.Replace(distFolder, string.Empty).Replace("\\", "/");
                            revText = revJson.ToString(Formatting.None);

                            //保存rev
                            MainWindowVM.ShowStatus("保存映射文件...");
                            var revHash = MD5.GetTextHash(revText).Replace("-", "").ToLower().Substring(0, 10);
                            var revNewFile = renameReg.Replace(revFile, "-" + revHash + ".");
                            var revNewName = Path.GetFileName(revNewFile);
                            value = GetRevToken(revJson, revFile);
                            if (value == null)
                            {
                                MainWindowVM.MessageWarn("映射中找不到文件：rev-manifest.json");
                                return;
                            }
                            value.Value = revNewFile.Replace(distFolder, string.Empty).Replace("\\", "/");
                            revText = revJson.ToString(Formatting.None);

                            File.WriteAllText(revNewFile, revText, Encoding.UTF8);

                            //更新Index
                            MainWindowVM.ShowStatus("更新主页...");
                            updates.Add(revFile, revNewFile);
                            updates = updates.Where(kv =>
                            kv.Key.Contains(@"\dist\rev-manifest-")
                            || kv.Key.Contains(@"\dist\app\common\style\startloader-")
                            || kv.Key.Contains(@"\dist\app\sys_const-")
                            || kv.Key.Contains(@"\dist\scripts\require-")
                            || kv.Key.Contains(@"\dist\app\loadRevManifest-")
                            || kv.Key.Contains(@"\dist\app\main-")).ToDictionary(kv => Path.GetFileName(kv.Key), kv => Path.GetFileName(kv.Value));

                            ReplaceIndexText(Path.Combine(distFolder, @"..\Views\dist\Home\index.cshtml"), updates);
                            ReplaceIndexText(Path.Combine(distFolder, @"..\Views\dist\Sub\index.cshtml"), updates);
                            ReplaceIndexText(Path.Combine(distFolder, @"..\Views\dist\Public\index.cshtml"), updates);

                            //更新Rev节点
                            var revNode = RevFileTree.Children?.FirstOrDefault()?.Children?.FirstOrDefault(n => n.Name == "rev-manifest.json")?.Children?.FirstOrDefault();
                            if (revNode != null)
                            {
                                RevFileTree.Path = revNewFile;
                                revNode.Path = revNewFile;
                                revNode.Name = revNewName;
                            }

                            MainWindowVM.NotifyInfo("更新完成");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("文件更新错误", ex);
                            MainWindowVM.NotifyError("更新错误：" + ex.Message);
                        }
                        finally
                        {
                            Doing = false;
                            MainWindowVM.ShowStatus();
                        }
                    });
                });
                return _UpdateFile;
            }
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
            //去除只读
            var attr = File.GetAttributes(filename);
            if (attr.HasFlag(FileAttributes.ReadOnly))
            {
                PathHelper.RemoveAttribute(filename, FileAttributes.ReadOnly);
            }
            File.WriteAllText(filename, text, Encoding.UTF8);
        }
        private JValue GetRevToken(JObject json, string file)
        {
            var distFolder = Path.Combine(WebsitePath, "dist") + "\\";
            file = file.Replace(distFolder, string.Empty).Replace("\\", "/");
            var maps = file.Split('/');
            JToken token = json;
            for (int i = 0; i < maps.Length; i++)
            {
                var map = maps[i];
                if(i == maps.Length - 1)
                {
                    map = Regex.Replace(map, @"-[0-9a-f]{10}\.", ".");
                }
                token = token?[map.ToLower()];
            }
            return token as JValue;
        }
        #endregion

        #region 复制
        private ICommand _CopyNode;
        public ICommand CopyNode
        {
            get
            {
                _CopyNode = _CopyNode ?? new RelayCommand<RevNodeItem>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (_ == null) return;
                    var text = _.Path;
                    if (string.IsNullOrEmpty(text)) return;
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        Clipboard.SetText(text);
                    });
                    NotifyWin.Info("已复制到剪贴板：" + text, "复制");
                });
                return _CopyNode;
            }
        }
        #endregion

        #region 查找
        public string FindText { get; set; } = string.Empty;
        private ICommand _Find;
        public ICommand Find
        {
            get
            {
                _Find = _Find ?? new RelayCommand<RevNodeItem>(_ =>
                {
                    if (string.IsNullOrEmpty(FindText)) return;
                    var result = FindNextItem(null, FindText);
                    if (result == null)
                    {
                        NotifyWin.Warn("未找到任何结果", "查找");
                    }
                });
                return _Find;
            }
        }
        private ICommand _FindDialog;
        public ICommand FindDialog
        {
            get
            {
                _FindDialog = _FindDialog ?? new RelayCommand<RevNodeItem>(_ =>
                {
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        var text = MessageWin.Input(FindText);
                        if (string.IsNullOrEmpty(text)) return;
                        FindText = text;
                        Find.Execute(_);
                    });
                });
                return _FindDialog;
            }
        }
        private ICommand _FindNext;
        public ICommand FindNext
        {
            get
            {
                _FindNext = _FindNext ?? new RelayCommand<RevNodeItem>(_ =>
                {
                    if (string.IsNullOrEmpty(FindText)) return;
                    _ = _ ?? GetSelectedItem();
                    var result = FindNextItem(_, FindText);
                    if (result == null)
                    {
                        MainWindowVM.NotifyWarn("未找到下一个", "查找");
                    }
                });
                return _FindNext;
            }
        }
        private ICommand _FindPrev;
        public ICommand FindPrev
        {
            get
            {
                _FindPrev = _FindPrev ?? new RelayCommand<RevNodeItem>(_ =>
                {
                    if (string.IsNullOrEmpty(FindText)) return;
                    _ = _ ?? GetSelectedItem();
                    var result = FindNextItem(_, FindText, true);
                    if (result == null)
                    {
                        MainWindowVM.NotifyWarn("未找到上一个", "查找");
                    }
                });
                return _FindPrev;
            }
        }

        private RevNodeItem GetSelectedItem(RevNodeItem node = null)
        {
            node = node ?? RevFileTree;
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
        private RevNodeItem FindNextItem(RevNodeItem item, string findText, bool previous = false)
        {
            item = item ?? RevFileTree;
            RevNodeItem result = null;
            if (previous)
            {
                result = FindParentNext(item, findText, previous);
                return result;
            }
            else
            {
                if (item.Children?.Any() ?? false)
                {
                    var list = previous ? item.Children.Reverse() : item.Children;
                    foreach (var child in list)
                    {
                        result = FindInItem(child, findText, previous);
                        if (result != null)
                        {
                            item.IsExpanded = true;
                            return result;
                        }
                    }
                }
                return FindParentNext(item, findText, previous);
            }
        }
        private RevNodeItem FindParentNext(RevNodeItem item, string findText, bool previous = false)
        {
            RevNodeItem result = null;
            var parent = GetParentItem(item);
            if (parent != null)
            {
                var startIndex = parent.Children.IndexOf(item) + 1;
                var list = previous ? parent.Children.Take(startIndex - 1).Reverse() : parent.Children.Skip(startIndex);
                foreach (var child in list)
                {
                    result = FindInItem(child, findText, previous);
                    if (result != null)
                    {
                        parent.IsExpanded = true;
                        return result;
                    }
                }
                //向上查找父级节点
                if (previous && FoundItem(parent, findText))
                {
                    parent.IsSelected = true;
                    return parent;
                }
                return FindParentNext(parent, findText, previous);
            }
            return result;
        }
        private RevNodeItem FindInItem(RevNodeItem item, string findText, bool previous = false)
        {
            RevNodeItem result = null;
            //查找下一个时,先查找父级
            if (!previous && FoundItem(item, findText))
            {
                item.IsSelected = true;
                return item;
            }
            if (item.Children?.Any() ?? false)
            {
                var list = previous ? item.Children.Reverse() : item.Children;
                foreach (var child in list)
                {
                    result = FindInItem(child, findText, previous);
                    if (result != null)
                    {
                        item.IsExpanded = true;
                        return result;
                    }
                }
            }
            //查找上一个时,后查找父级
            if (previous && FoundItem(item, findText))
            {
                item.IsSelected = true;
                return item;
            }
            return result;
        }
        private RevNodeItem GetParentItem(RevNodeItem item, RevNodeItem node = null)
        {
            node = node ?? RevFileTree;
            foreach (var child in node.Children)
            {
                if (child.Equals(item)) return node;
                RevNodeItem result = GetParentItem(item, child);
                if (result != null) return result;
            }
            return null;
        }
        private bool FoundItem(RevNodeItem item, string findText)
        {
            if (item.Name?.ToLower().Contains(findText.ToLower()) ?? false)
            {
                return true;
            }
            return false;
        }
        #endregion

        #region Setting
        public void ReadSetting()
        {
            InputWebsitePath = WebsitePath = MainWindowVM.ReadSetting($"{nameof(DistEditorCtrl)}.{nameof(WebsitePath)}", WebsitePath);
            LoadRev.Execute(null);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(DistEditorCtrl)}.{nameof(WebsitePath)}", WebsitePath);
        }
        #endregion
    }
}
