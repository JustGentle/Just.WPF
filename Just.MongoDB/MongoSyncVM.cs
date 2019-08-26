﻿using GenLibrary.MVVM.Base;
using Just.Base;
using Just.Base.Utils;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using MongoDB.Bson;
using MongoDB.Driver;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Just.MongoDB
{
    [AddINotifyPropertyChangedInterface]
    public class MongoSyncVM
    {
        public bool Doing { get; set; }
        public string JsonPath { get; set; } = Directory.GetCurrentDirectory();
        public MongoNode Tree { get; set; } = new MongoNode();

        #region 视图
        public bool IsJsonView { get; set; } = false;
        private ICommand _SwitchView = null;
        public ICommand SwitchView
        {
            get
            {
                _SwitchView = _SwitchView ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    IsJsonView = !IsJsonView;
                });
                return _SwitchView;
            }
        }
        #endregion

        #region 读取
        public string Json { get; set; }
        public ObservableCollection<CacheSysProfileMode> SysProfiles { get; set; }

        private ICommand _JsonFileBrowser;
        public ICommand JsonFileBrowser
        {
            get
            {
                _JsonFileBrowser = _JsonFileBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        Multiselect = true
                    };
                    dlg.Filters.Add(new CommonFileDialogFilter("脚本文件", "*.txt;*.json"));

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.JsonPath = string.Join(",", dlg.FileNames);
                    }
                });
                return _JsonFileBrowser;
            }
        }

        private ICommand _JsonFolderBrowser;
        public ICommand JsonFolderBrowser
        {
            get
            {
                _JsonFolderBrowser = _JsonFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.JsonPath = dlg.FileName;
                    }
                });
                return _JsonFolderBrowser;
            }
        }

        private ICommand _CopyJson;
        public ICommand CopyJson
        {
            get
            {
                _CopyJson = _CopyJson ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrEmpty(Json)) return;
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        Clipboard.SetText(Json);
                        NotifyWin.Info("已复制到剪贴板", "读取结果");
                    });
                });
                return _CopyJson;
            }
        }

        private StringBuilder json;
        private ICommand _ReadJson;
        public ICommand ReadJson
        {
            get
            {
                _ReadJson = _ReadJson ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        if (string.IsNullOrWhiteSpace(JsonPath))
                        {
                            MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("脚本路径不能为空"); });
                            return;
                        }
                        MainWindowVM.ShowStatus("开始读取...");
                        Doing = true;
                        try
                        {
                            json = new StringBuilder();
                            SysProfiles = new ObservableCollection<CacheSysProfileMode>();
                            Scan(JsonPath);
                            if (json.Length > 0) Json = Format(json.ToString());
                            CopyJson.Execute(_);
                            MainWindowVM.ShowStatus("加载树...");
                            MainWindowVM.DispatcherInvoke(() => { Tree = new MongoNode(); });
                            if (SysProfiles.Any())
                            {
                                var node = AddTreeNode("[读取结果]", SysProfiles, Tree);
                                node.Children = new MongoNodeCollection(node.Children.ToList().OrderBy(n => n.Key));
                                node.SetEnableByChildren();
                                node.IsExpanded = true;
                            }
                            else
                            {
                                SysProfiles = null;
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.Error("脚本读取错误", ex);
                            MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("读取错误：" + ex.Message); });
                        }
                        Doing = false;
                        MainWindowVM.ShowStatus();
                    });
                });
                return _ReadJson;
            }
        }

        private void Scan(string path)
        {
            MainWindowVM.ShowStatus("扫描..." + path);
            if (path.Contains(",") || File.Exists(path))
            {
                var fs = path.Split(',');
                foreach (var item in fs)
                {
                    ScanFile(item);
                }
                return;
            }
            if (!Directory.Exists(path))
            {
                MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("路径不存在：" + JsonPath); });
                return;
            }
            var folders = Directory.GetDirectories(path);
            foreach (var item in folders)
            {
                Scan(item);
            }
            var files = Directory.GetFiles(path, "*.txt");
            foreach (var item in files)
            {
                ScanFile(item);
            }
            files = Directory.GetFiles(path, "*.json");
            foreach (var item in files)
            {
                ScanFile(item);
            }
        }
        private void ScanFile(string file)
        {
            var endcoding = EncodingGetter.GetEncoding(file);
            var text = File.ReadAllText(file, endcoding);
            text = text.Trim();

            var caches = ReadMongoJson(text);
            foreach (var cache in caches)
            {
                if (string.IsNullOrEmpty(cache.Mode) || string.IsNullOrEmpty(cache.Item))
                {
                    continue;
                }
                if (!SysProfiles.Any(c => Equals(c, cache)))
                {
                    MainWindowVM.DispatcherInvoke(() => { SysProfiles.Add(cache); });
                    //移除_id
                    var j = MongoDBHelper.ToJson(cache);
                    j = Regex.Replace(j, @"\s*""_id""[^\r\n]*", "");
                    json.AppendLine(j);
                }
            }
        }

        private string Format(string text)
        {
            //去除首尾空
            text = text.Trim();
            //去除多行注释
            text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
            //去除单行注释
            text = Regex.Replace(text, @"^\s*//[^\r\n]*$", "", RegexOptions.Multiline);
            return text;
        }
        private List<CacheSysProfileMode> ReadMongoJson(string json)
        {
            var result = MongoDBHelper.FromManyJson<CacheSysProfileMode>(json);
            return result;
        }
        #endregion

        #region 同步
        public string MongoDBAddress { get; set; } = "localhost:27017";
        public bool IsAdd { get; set; } = true;
        public bool IsUpdate { get; set; } = true;
        public bool IsRemoveDup { get; set; } = true;
        public bool IsDeleteOver { get; set; } = false;
        public bool IsShowSame { get; set; } = true;
        public bool HasDBAction
        {
            get
            {
                return IsAdd || IsUpdate || IsRemoveDup || IsDeleteOver;
            }
        }

        private MongoDBHelper mongo = null;

        private ICommand _execute;
        public ICommand Execute
        {
            get
            {
                _execute = _execute ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (!SysProfiles?.Any() ?? true) return;
                    if (Tree.Children[0].IsEnable != true)
                    {
                        var node = Tree.Children[0].Children.FirstOrDefault(n => n.IsEnable != true);
                        if (node != null) node.IsSelected = true;
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("数据冲突，请修正后再执行！"); });
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(MongoDBAddress))
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("链接地址不能为空"); });
                        return;
                    }
                    Doing = true;
                    MainWindowVM.ShowStatus("同步...");
                    try
                    {
                        if (!HasDBAction || MessageWin.Confirm("同步将会立刻影响系统数据，执行前自动【完整备份】，确定同步？") == true)
                        {
                            //查询原数据
                            var timeoutMS = 3000;
                            mongo = new MongoDBHelper(
                                $"mongodb://{MongoDBAddress}/?serverSelectionTimeoutMS={timeoutMS};connectTimeoutMS={timeoutMS};socketTimeoutMS={timeoutMS}",
                                "iOffice10Cache",
                                nameof(CacheSysProfileMode));
                            var collection = mongo.Find<CacheSysProfileMode>();
                            //备份
                            Backup(collection, nameof(CacheSysProfileMode));

                            MainWindowVM.ShowStatus("执行...");
                            //初始化
                            InitExecutionTree();
                            var sames = new List<CacheSysProfileMode>();
                            var dups = new List<CacheSysProfileMode>();
                            var news = new List<CacheSysProfileMode>();
                            //对比差异
                            collection.ForEach(next =>
                            {
                                CacheSysProfileMode found = null;
                                for (int i = 0; i < SysProfiles.Count; i++)
                                {
                                    var item = SysProfiles[i];
                                    if (Same(item, next))
                                    {
                                        if (found == null)
                                        {
                                            if (sames.Contains(item))
                                            {
                                                OnDup(item, next);
                                            }
                                            else
                                            {
                                                if (Equals(item, next))
                                                    OnEq(item, next);
                                                else
                                                    OnDiff(item, next);
                                                sames.Add(item);
                                            }
                                            found = next;
                                        }
                                        else
                                        {
                                            dups.Add(item);
                                            item.Id = ObjectId.Empty;
                                            OnDup(found, item);
                                        }
                                    }
                                }
                                if (found == null)
                                    OnNot(next);
                            });
                            //新增
                            for (int i = 0; i < SysProfiles.Count; i++)
                            {
                                var item = SysProfiles[i];
                                if (sames.Contains(item) || dups.Contains(item)) continue;
                                CacheSysProfileMode dup = news.FirstOrDefault(e => Same(item, e));
                                if (dup != null)
                                {
                                    OnDup(dup, item);
                                }
                                else
                                {
                                    OnAdd(item);
                                    news.Add(item);
                                }
                            }
                            //统计数量
                            for (int i = 0; i < Tree.Children.Count; i++)
                            {
                                UpdateValueByChildrenCount(Tree.Children[i]);
                            }
                            MainWindowVM.DispatcherInvoke(() => { NotifyWin.Info(HasDBAction ? "同步成功" : "检查完成"); });
                        }
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Error("MongoDB连接超时", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("连接超时！", "同步错误"); });
                    }
                    catch (MongoConfigurationException ex)
                    {
                        Logger.Error("MongoDB链接地址错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("请检查【链接地址】是否正确！\n" + ex.Message, "同步错误"); });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("MongoDB同步错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error(ex.Message, "同步错误"); });
                    }
                    MainWindowVM.ShowStatus();
                    Doing = false;
                });
                return _execute;
            }
        }

        private void InitExecutionTree()
        {
            Tree = new MongoNode();
            AddTreeNode("新增", new List<object>(), Tree);
            AddTreeNode("更新", new object(), Tree);
            AddTreeNode("重复", new object(), Tree);
            AddTreeNode("多余", new List<object>(), Tree);
            AddTreeNode("相同", new List<object>(), Tree);
        }
        private void Backup(IEnumerable<CacheSysProfileMode> collection, string collectionName)
        {
            if (!HasDBAction) return;
            if (!collection?.Any() ?? true) return;
            MainWindowVM.ShowStatus("备份...");
            var names = mongo.Database.ListCollectionNames().ToList().Where(n => n.StartsWith(collectionName));
            var bak = $"{collectionName}_{DateTime.Now:yyMMddHHmmssfff}";
            while (names.Contains(bak))
            {
                if (bak.EndsWith("".PadRight(10, '_')))
                {
                    throw new Exception("备份失败:" + bak);
                }
                bak += "_";
            }
            mongo.Database.CreateCollection(bak);
            mongo.CollectionName = bak;
            mongo.InsertMany(collection);
            mongo.CollectionName = collectionName;
        }
        private bool Same(CacheSysProfileMode item, CacheSysProfileMode next)
        {
            return (string.Equals(item.Mode, next.Mode, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(item.Item, next.Item, StringComparison.CurrentCultureIgnoreCase));
        }
        private bool Equals(CacheSysProfileMode item, CacheSysProfileMode eqItem)
        {
            return !(item.Mode != eqItem.Mode
                || item.Item != eqItem.Item
                || item.Display != eqItem.Display
                || item.Hints != eqItem.Hints
                || item.DefaultValue != eqItem.DefaultValue
                || item.ValueType != eqItem.ValueType
                || item.Category != eqItem.Category
                || item.Visible != eqItem.Visible
                || item.EnumValue?.ToJson() != eqItem.EnumValue?.ToJson()
                || item.DynamicValue?.ToJson() != eqItem.DynamicValue?.ToJson()
            );
        }

        private void OnAdd(CacheSysProfileMode item)
        {
            item.Id = ObjectId.Empty;
            if (IsAdd)
            {
                mongo.InsertOne(item);
            }

            if (IsAdd || !HasDBAction)
            {
                AddTreeNode(null, item, Tree["新增"]);
            }
        }
        private void OnDup(CacheSysProfileMode item, CacheSysProfileMode dup)
        {
            if (IsRemoveDup && dup.Id != ObjectId.Empty)
            {
                mongo.DeleteOne<CacheSysProfileMode>(e => e.Id == dup.Id);
            }

            if (IsRemoveDup || !HasDBAction)
            {
                var key = item.Mode + ':' + item.Item;
                var parent = Tree["重复"];
                var child = parent[key];
                if (child == null)
                {
                    AddTreeNode(key, new { 保留 = item, 移除 = new List<CacheSysProfileMode>() }, parent).IsExpanded = true; ;
                }
                child = parent[key]["移除"];
                AddTreeNode($"[{child.Children.Count}]", dup, child);
                UpdateValueByChildrenCount(child);
            }
        }
        private void OnDiff(CacheSysProfileMode item, CacheSysProfileMode diff)
        {
            item.Id = ObjectId.Empty;
            if (IsUpdate)
            {
                var copy = MongoDBHelper.FromJson<CacheSysProfileMode>(item.ToJson());
                if (copy.ValueType == diff.ValueType)
                {
                    copy.ItemValue = diff.ItemValue;
                }
                copy.Id = diff.Id;
                mongo.ReplaceOne(e => e.Id == diff.Id, copy);
            }
            if (IsUpdate || !HasDBAction)
            {
                var key = item.Mode + ':' + item.Item;
                var parent = Tree["更新"];
                AddTreeNode(key, new { 原 = diff, 新 = item }, parent).IsExpanded = true;
            }
        }
        private void OnNot(CacheSysProfileMode not)
        {
            if (IsDeleteOver)
            {
                mongo.DeleteOne<CacheSysProfileMode>(e => e.Id == not.Id);
            }

            if (IsDeleteOver || !HasDBAction)
            {
                var parent = Tree["多余"];
                AddTreeNode(null, not, parent);
            }
        }
        private void OnEq(CacheSysProfileMode item, CacheSysProfileMode eq)
        {
            if (IsShowSame || !HasDBAction)
            {
                var parent = Tree["相同"];
                AddTreeNode(null, eq, parent);
            }
        }
        #endregion

        #region 树节点
        private MongoNode AddTreeNode(string key, object value, MongoNode parent)
        {
            if (string.IsNullOrEmpty(key))
            {
                if (value is CacheSysProfileMode cache)
                {
                    key = $"{cache.Mode}:{cache.Item}";
                }
                else
                {
                    key = $"[{parent.Children.Count}]";
                }
            }
            var node = new MongoNode { Key = key, IsEnable = parent.IsEnable ?? true };
            if (value == null)
            {
                node.Value = "null";
                node.Type = "Null";
                node.ImagePath = @"\Images\null.png";
            }
            else if (value is ObjectId id)
            {
                if (id == ObjectId.Empty)
                {
                    node = null;
                }
                else
                {
                    node.Value = value.ToString();
                    node.Type = value.GetType().Name;
                    node.ImagePath = @"\Images\id.png";
                }
            }
            else if (value is string || value is bool || value is int || value is long || value is double || value is decimal || value is ObjectId)
            {
                node.Value = value.ToString();
                node.Type = value.GetType().Name;
                node.ImagePath = @"\Images\id.png";
            }
            else if (value is DateTime t)
            {
                node.Value = t.ToString("yyyy-MM-dd HH:mm:ss.fff");
                node.Type = value.GetType().Name;
                node.ImagePath = @"\Images\id.png";
            }
            else if (value is System.Collections.ICollection c)
            {
                node.Value = $"[{c.Count}]";
                node.Type = nameof(Array);
                node.ImagePath = @"\Images\arr.png";
                var i = 0;
                foreach (var item in c)
                {
                    AddTreeNode(null, item, node);
                    i++;
                }
            }
            else if (value is BsonDocument d)
            {
                node.Value = $"{{{d.ElementCount}}}";
                node.Type = nameof(Object);
                node.ImagePath = @"\Images\obj.png";
                foreach (var item in d.Elements)
                {
                    AddTreeNode(item.Name, item.Value, node);
                }
            }
            else if (value is BsonValue v)
            {
                node = AddBsonValueNode(key, v, parent);
                if (node != null) node = null;
            }
            else
            {
                var props = value.GetType().GetProperties();
                node.Value = string.Empty;// $"{{{props.Length}}}";
                node.ImagePath = @"\Images\obj.png";
                node.Type = nameof(Object);
                if (value is CacheSysProfileMode cache)
                {
                    node.Value += cache.Display;
                    var exists = parent.Children.Where(n => n.Key == node.Key);
                    if (exists.Any())
                    {
                        foreach (var item in exists)
                        {
                            item.IsEnable = false;
                            item.Foreground = (SolidColorBrush)Application.Current.FindResource("RedBrush");
                        }
                        node.IsEnable = false;
                        node.Foreground = (SolidColorBrush)Application.Current.FindResource("RedBrush");
                    }
                }
                foreach (var item in props)
                {
                    AddTreeNode(item.Name, item.GetValue(value), node);
                }
            }
            if (node != null) MainWindowVM.DispatcherInvoke(() =>
            {
                parent.Children.Add(node);
            });
            return node;
        }
        private MongoNode AddBsonValueNode(string key, BsonValue v, MongoNode parent)
        {
            MongoNode node = new MongoNode { Key = key };
            if (v.IsBsonNull)
                node = AddTreeNode(key, null, parent);
            else if (v.IsBoolean)
                node = AddTreeNode(key, v.ToBoolean(), parent);
            else if (v.IsInt32)
                node = AddTreeNode(key, v.ToInt32(), parent);
            else if (v.IsInt32 || v.IsInt64)
                node = AddTreeNode(key, v.ToInt64(), parent);
            else if (v.IsDouble)
                node = AddTreeNode(key, v.ToDouble(), parent);
            else if (v.IsDecimal128 || v.IsNumeric)
                node = AddTreeNode(key, v.ToDecimal(), parent);
            else if (v.IsObjectId)
                node = AddTreeNode(key, v.AsObjectId, parent);
            else if (v.IsString || v.IsGuid)
                node = AddTreeNode(key, v.ToString(), parent);
            else if (v.IsValidDateTime || v.IsBsonDateTime)
                node = AddTreeNode(key, v.ToLocalTime(), parent);
            else if (v.IsString)
                node = AddTreeNode(key, v.ToString(), parent);
            else if (v.IsValidDateTime)
                node = AddTreeNode(key, v.ToLocalTime(), parent);
            else if (v.IsBsonArray)
            {
                var array = v.AsBsonArray;
                node.Value = $"[{array.Count}]";
                node.Type = "Array";
                node.ImagePath = @"\Images\arr.png";
                var i = 0;
                foreach (var item in array)
                {
                    AddBsonValueNode($"[{i}]", item, node);
                    i++;
                }
            }
            else
                node = AddTreeNode(key, v.ToString(), parent);

            return node;
        }
        private string UpdateValueByChildrenCount(MongoNode node)
        {
            if (node.Type == nameof(Array))
            {
                node.Value = $"[{node.Children.Count}]";
            }
            else if (node.Type == nameof(Object))
            {
                node.Value = $"{{{node.Children.Count}}}";
            }
            return node.Value;
        }
        #endregion

        #region 导出脚本
        private ICommand _ExportDB;
        public ICommand ExportDB
        {
            get
            {
                _ExportDB = _ExportDB ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    if (string.IsNullOrWhiteSpace(MongoDBAddress))
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("链接地址不能为空"); });
                        return;
                    }
                    MainWindowVM.ShowStatus("读取...");
                    try
                    {
                        //读取数据
                        var timeoutMS = 3000;
                        mongo = new MongoDBHelper(
                            $"mongodb://{MongoDBAddress}/?serverSelectionTimeoutMS={timeoutMS};connectTimeoutMS={timeoutMS};socketTimeoutMS={timeoutMS}",
                            "iOffice10Cache",
                            nameof(CacheSysProfileMode));
                        var collection = mongo.Find<CacheSysProfileMode>();
                        //保存数据
                        MainWindowVM.ShowStatus("生成...");
                        var sb = new StringBuilder();
                        foreach (var item in collection)
                        {
                            //移除_id
                            var j = MongoDBHelper.ToJson(item);
                            j = Regex.Replace(j, @"\s*""_id""[^\r\n]*", "");
                            sb.AppendLine(j);
                        }
                        var json = sb.ToString();
                        if (string.IsNullOrEmpty(json))
                        {
                            MainWindowVM.DispatcherInvoke(() =>
                            {
                                NotifyWin.Warn("当前数据为空", "导出脚本");
                            });
                        }
                        else
                        {
                            MainWindowVM.ShowStatus("保存...");
                            var sfd = new CommonSaveFileDialog($"导出脚本 - {MongoDBAddress}")
                            {
                                DefaultFileName = $"Mongo - {DateTime.Now:yyMMddHHmmssfff}",
                                DefaultExtension = ".txt"
                            };
                            sfd.Filters.Add(new CommonFileDialogFilter("文本文件", "*.txt"));
                            sfd.Filters.Add(new CommonFileDialogFilter("脚本文件", "*.json"));
                            sfd.Filters.Add(new CommonFileDialogFilter("所有文件", "*.*"));
                            if (sfd.ShowDialog(MainWindowVM.Instance.MainWindow) == CommonFileDialogResult.Ok)
                            {
                                var fileName = sfd.FileName;
                                File.WriteAllBytes(fileName, Encoding.UTF8.GetBytes(json));
                                MainWindowVM.DispatcherInvoke(() =>
                                {
                                    NotifyWin.Info("导出完成", "导出脚本");
                                });
                            }
                        }
                    }
                    catch (TimeoutException ex)
                    {
                        Logger.Error("MongoDB连接超时", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("连接超时！", "导出脚本"); });
                    }
                    catch (MongoConfigurationException ex)
                    {
                        Logger.Error("MongoDB链接地址错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error("请检查【链接地址】是否正确！\n" + ex.Message, "导出脚本"); });
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("导出脚本错误", ex);
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Error(ex.Message, "导出脚本"); });
                    }
                    MainWindowVM.ShowStatus();
                });
                return _ExportDB;
            }
        }
        #endregion

        #region 菜单
        //查找
        public static string FindText { get; set; } = string.Empty;
        private ICommand _Find;
        public ICommand Find
        {
            get
            {
                _Find = _Find ?? new RelayCommand<MongoNode>(_ =>
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
                _FindDialog = _FindDialog ?? new RelayCommand<MongoNode>(_ =>
                {
                    if (IsJsonView) return;
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
                _FindNext = _FindNext ?? new RelayCommand<MongoNode>(_ =>
                {
                    if (IsJsonView) return;
                    if (string.IsNullOrEmpty(FindText)) return;
                    _ = _ ?? GetSelectedItem();
                    var result = FindNextItem(_, FindText);
                    if (result == null)
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("未找到下一个", "查找"); });
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
                _FindPrev = _FindPrev ?? new RelayCommand<MongoNode>(_ =>
                {
                    if (IsJsonView) return;
                    if (string.IsNullOrEmpty(FindText)) return;
                    _ = _ ?? GetSelectedItem();
                    var result = FindNextItem(_, FindText, true);
                    if (result == null)
                    {
                        MainWindowVM.DispatcherInvoke(() => { NotifyWin.Warn("未找到上一个", "查找"); });
                    }
                });
                return _FindPrev;
            }
        }
        private MongoNode GetSelectedItem(MongoNode node = null)
        {
            node = node ?? Tree;
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
        private MongoNode FindNextItem(MongoNode item, string findText, bool previous = false)
        {
            item = item ?? Tree;
            MongoNode result = null;
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
        private MongoNode FindParentNext(MongoNode item, string findText, bool previous = false)
        {
            MongoNode result = null;
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
        private MongoNode FindInItem(MongoNode item, string findText, bool previous = false)
        {
            MongoNode result = null;
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
        private MongoNode GetParentItem(MongoNode item, MongoNode node = null)
        {
            MongoNode result = null;
            node = node ?? Tree;
            foreach (var child in node.Children)
            {
                if (child.Equals(item)) return node;
                result = GetParentItem(item, child);
                if (result != null) return result;
            }
            return null;
        }
        private bool FoundItem(MongoNode item, string findText)
        {
            if (item.Key?.ToLower().Contains(findText.ToLower()) ?? false)
            {
                return true;
            }
            if (item.Value?.ToLower().Contains(findText.ToLower()) ?? false)
            {
                return true;
            }
            return false;
        }

        //复制节点脚本
        private ICommand _CopyNodeJson;
        public ICommand CopyNodeJson
        {
            get
            {
                _CopyNodeJson = _CopyNodeJson ?? new RelayCommand<MongoNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (_ == null) return;
                    var json = GetNodeJson(_, GetNodeLevel(_));
                    if (string.IsNullOrEmpty(json)) return;
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        Clipboard.SetText(json);
                        NotifyWin.Info("已复制到剪贴板", "复制节点脚本");
                    });
                });
                return _CopyNodeJson;
            }
        }
        //查看节点脚本
        private ICommand _ShowNodeJson;
        public ICommand ShowNodeJson
        {
            get
            {
                _ShowNodeJson = _ShowNodeJson ?? new RelayCommand<MongoNode>(_ =>
                {
                    _ = _ ?? GetSelectedItem();
                    if (_ == null) return;
                    var json = GetNodeJson(_, GetNodeLevel(_));
                    if (string.IsNullOrEmpty(json))
                    {
                        MainWindowVM.DispatcherInvoke(() =>
                        {
                            NotifyWin.Warn("节点脚本为空", "查看节点脚本");
                        });
                    }
                    MainWindowVM.DispatcherInvoke(() =>
                    {
                        var n = new MessageWin
                        {
                            Title = _.Key.Contains(":") ? _.Value : _.Key, //CacheSysProfileMode节点标题显示Display
                            Message = json,
                            OkContent = "复制",
                            CancelContent = "关闭",
                            IsConfirm = true,
                            MessageAlignment = HorizontalAlignment.Left,
                            Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                            Width = 500,
                            Owner = MainWindowVM.Instance.MainWindow
                        };
                        if (n.ShowDialog() == true)
                        {
                            Clipboard.SetText(json);
                            NotifyWin.Info("已复制到剪贴板", "复制节点脚本");
                        }
                    });
                });
                return _ShowNodeJson;
            }
        }
        private int GetNodeLevel(MongoNode node)
        {
            /* -1: 根节点 - 不复制Key, 不加OpenClose
             * 1: 叶子节点 - 复制Key, 加OpenClose
             * 0: 其他节点 - 不复制Key, 加OpenClose
             * */
            return node.Key == "[读取结果]" ? -1 : (node.Children?.Any() ?? false) ? 0 : 1;
        }
        private string GetNodeJson(MongoNode node, int lv = 0)
        {
            if (node == null) return string.Empty;

            var sb = new StringBuilder();
            //每级缩进两个空格
            var lvSpace = new String(' ', Math.Max(0, lv * 2));
            if (lv > 0) sb.Append($"{lvSpace}\"{node.Key}\":");

            //不同类型不同起止字符
            string valueOpen = "", valueClose = "";
            if (lv >= 0)
                switch (node.Type)
                {
                    case nameof(Object):
                        valueOpen = "{";
                        valueClose = "}";
                        break;
                    case nameof(Array):
                        valueOpen = "[";
                        valueClose = "]";
                        break;
                    case nameof(ObjectId):
                        valueOpen = "ObjectId(\"";
                        valueClose = "\")";
                        break;
                    case nameof(DateTime):
                        valueOpen = "ISODate(\"";
                        valueClose = "\")";
                        break;
                    case nameof(Boolean):
                    case nameof(Int32):
                    case nameof(Int64):
                    case nameof(Double):
                    case nameof(Decimal):
                    case nameof(Decimal128):
                    case "int":
                    case "long":
                    case "decimal":
                    case "bool":
                    case "null":
                        break;
                    default:
                        valueOpen = valueClose = "\"";
                        break;
                }

            sb.Append(valueOpen);
            var value = string.Empty;
            if (node.Children?.Any() ?? false)
            {
                var itemJsons = new List<string>();
                foreach (var item in node.Children)
                {
                    itemJsons.Add(GetNodeJson(item, lv + 1));
                }
                //负数节点(即不复制的节点)不需要逗号分隔
                var separator = lv >= 0 ? "," + Environment.NewLine : Environment.NewLine;
                value = string.Join(separator, itemJsons);
                if (!string.IsNullOrEmpty(value))
                {
                    //负数节点第一个子节点不需要换行
                    if (lv >= 0) value = $"{Environment.NewLine}{lvSpace}" + value;
                    value += $"{Environment.NewLine}{lvSpace}";
                }
            }
            else
            {
                value = node.Value;
            }
            sb.Append(value);
            sb.Append(valueClose);

            return sb.ToString();
        }
        #endregion

        #region Setting
        public void ReadSetting()
        {
            JsonPath = MainWindowVM.ReadSetting($"{nameof(MongoSyncCtrl)}.{nameof(JsonPath)}", JsonPath);
            MongoDBAddress = MainWindowVM.ReadSetting($"{nameof(MongoSyncCtrl)}.{nameof(MongoDBAddress)}", MongoDBAddress);
            IsAdd = MainWindowVM.ReadSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsAdd)}", IsAdd);
            IsUpdate = MainWindowVM.ReadSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsUpdate)}", IsUpdate);
            IsRemoveDup = MainWindowVM.ReadSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsRemoveDup)}", IsRemoveDup);
            //IsDeleteOver = MainWindowVM.ReadSetting($"{nameof(MongoSync)}.{nameof(IsDeleteOver)}", IsDeleteOver);
            IsShowSame = MainWindowVM.ReadSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsShowSame)}", IsShowSame);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(JsonPath)}", JsonPath);
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(MongoDBAddress)}", MongoDBAddress);
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsAdd)}", IsAdd);
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsUpdate)}", IsUpdate);
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsRemoveDup)}", IsRemoveDup);
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsDeleteOver)}", IsDeleteOver);
            MainWindowVM.WriteSetting($"{nameof(MongoSyncCtrl)}.{nameof(IsShowSame)}", IsShowSame);
        }
        #endregion
    }
}