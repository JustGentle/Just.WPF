using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using GenLibrary.MVVM.Base;
using System.Data;
namespace GenLibrary.MVVM.TreeListView
{
    public class TreeListDataTable : ViewModelBase
    {
        //声明委托变量，外部可以对其设定
        DelAfterSelectedList AfterTreeListItemSelected = null;
        #region 构造函数
        //记录数据源
        DataView _DataSource = null;
        /// <summary>
        /// 创建对应表键值的数据视图
        /// </summary>
        /// <param name="dtable">数据表</param>
        /// <param name="isUseDummyChild">设定是否使用延迟加载项</param>
        public TreeListDataTable(DataView dtable, string parentKey, string key, DelAfterSelectedList DelSel, bool isUseDummyChild = true, string isDummyChildField = "")
        {
            ParentKey = parentKey;
            Key = key;
            AfterTreeListItemSelected = DelSel;
            IsDummyChildField = isDummyChildField;
            //创建用于绑定的数据源
            _TreeRoot = new TreeListViewItemViewModel();
            //设定是否使用延迟加载项
            IsUseDummyChild = isUseDummyChild;
            _DataSource = dtable;//记录数据对象
            FillData();//填充数据
        }
        public TreeListDataTable(DataView dtable, string parentKey, string key, bool isUseDummyChild = true, string isDummyChildField = "")
        {
            ParentKey = parentKey;
            Key = key;
            IsDummyChildField = isDummyChildField;
            //创建用于绑定的数据源
            _TreeRoot = new TreeListViewItemViewModel();
            //设定是否使用延迟加载项
            IsUseDummyChild = isUseDummyChild;
            _DataSource = dtable;//记录数据对象
            FillData();//填充数据
        }
        //项被选择后执行
        void TreeListItemSelected(TreeListViewItemViewModel item)
        {
            //设定选择的全称路径
            if (_SelectedFullPath != item.FullPath)
            {
                _SelectedFullPath = item.FullPath;
                this.OnPropertyChanged(() => this.SelectedFullPath);
                _SelectedItem = item;
                this.OnPropertyChanged(() => this.SelectedItem);
            }
            //调用外部委托
            AfterTreeListItemSelected(item);
        }
        #endregion
        #region 属性定义
        bool _IsOpen = false;
        /// <summary>
        /// 用于ComboBox下拉框的弹出绑定
        /// </summary>
        public bool IsOpen
        {
            get { return _IsOpen; }
            set
            {
                if (value != _IsOpen)
                {
                    _IsOpen = value;
                    this.OnPropertyChanged(() => this.IsOpen);
                }
            }
        }

        string _SelectedFullPath = "";
        public string SelectedFullPath
        {
            get { return _SelectedFullPath; }
            set
            {
                if (value != _SelectedFullPath)
                {
                    _SelectedFullPath = value;
                    this.OnPropertyChanged(() => this.SelectedFullPath);
                    SetSelected(value);
                }
            }
        }
        string _IsDummyChildField = "";
        /// <summary>
        /// 设定/获取是否使用延迟加载数据的字段，一般为最底层的数据项设定非延迟加载
        /// 必须设定IsUseDummyChild=true,以及指定isDummyChildField，并对相应的表字段赋值才有效，
        /// 并且为了提供性能，直接终止了子项的添加，所以使用此功能的必须是最内层的数据
        /// </summary>
        public string IsDummyChildField
        {
            get { return _IsDummyChildField; }
            set
            {
                if (value != _IsDummyChildField)
                {
                    _IsDummyChildField = value;
                    this.OnPropertyChanged(() => this.IsDummyChildField);
                }
            }
        }

        bool _IsUseDummyChild = true;
        /// <summary>
        /// 设定/获取是否使用延迟加载数据
        /// </summary>
        public bool IsUseDummyChild
        {
            get { return _IsUseDummyChild; }
            set
            {
                if (value != _IsUseDummyChild)
                {
                    _IsUseDummyChild = value;
                    this.OnPropertyChanged(() => this.IsUseDummyChild);
                }
            }
        }

        string _ParentKey = "";
        /// <summary>
        /// 设定/获取项的父键值对应的数据表列名称
        /// </summary>
        public string ParentKey
        {
            get { return _ParentKey; }
            set
            {
                if (value != _ParentKey)
                    _ParentKey = value;
                this.OnPropertyChanged(() => this.ParentKey);
            }
        }
        string _Key = "";
        /// <summary>
        /// 设定/获取项的键值对应的数据表列名称
        /// </summary>
        public string Key
        {
            get { return _Key; }
            set
            {
                if (value != _Key)
                    _Key = value;
                this.OnPropertyChanged(() => this.Key);
            }
        }

        TreeListViewItemViewModel _TreeRoot = null;
        /// <summary>
        /// 返回用于控件绑定的数据视图
        /// </summary>
        public TreeListViewItemViewModel TreeRoot
        {
            get { return _TreeRoot; }
        }
        TreeListViewItemViewModel _SelectedItem = null;
        /// <summary>
        /// 被选择的项
        /// </summary>
        public TreeListViewItemViewModel SelectedItem
        {
            get { return _SelectedItem; }
        }
        #endregion
        #region 公有函数
        /// <summary>
        /// 清空所有的数据对象，释放资源
        /// </summary>
        public void Clear()
        {
            _SelectedItem = null;
            _TreeRoot.Children.Clear();
            AfterTreeListItemSelected = null;
            _TreeRoot = null;
            _DataSource = null;
        }
        //遍历搜索查找的方式
        bool SetSelect(TreeListViewItemViewModel data, string selFullName)
        {
            bool isSel = false;
            foreach (TreeListViewItemViewModel chd in data.Children)
            {
                if (chd.FullPath == selFullName)
                {
                    chd.IsSelected = true;
                    return true;
                }
                chd.IsExpanded = true;//展开子节点(可能加载子项)
                // 按需延时装载子项
                if (chd.HasDummyChild)
                {
                    AddSubNode(chd.DataRowView[Key].ToString(), chd);//加载子节点
                }

                isSel = SetSelect(chd, selFullName);
                if (isSel) return true;
            }
            return isSel;
        }
        //通过路径解析直接定位，速度更快
        bool SetSelected(TreeListViewItemViewModel parentTreeList, string selFullName, ref string currSubFullName)
        {
            string[] keys = currSubFullName.Split(new char[1] { '.' });//使用登号分隔符
            if (keys.Length < 1) return false;
            // 按需延时装载子项
            if (parentTreeList.HasDummyChild)
            {
                AddSubNode(parentTreeList.DataRowView[Key].ToString(), parentTreeList);//加载子节点
            }
            //获取子键
            string subKey = keys[0];
            bool isSel = false;
            foreach (TreeListViewItemViewModel chd in parentTreeList.Children)
            {
                if (chd.DataRowView[Key].ToString() == subKey) //搜寻项为当前项或者其内部的子项
                {
                    chd.Parent.IsExpanded = true;//当前项的父亲展开
                    if (chd.FullPath == selFullName) //搜索为当前项
                    {
                        chd.IsSelected = true;
                        return true;
                    }
                    else //搜索子项
                    {

                        //截取子项
                        if (subKey.Length >= currSubFullName.Length) return false;
                        currSubFullName = currSubFullName.Substring(subKey.Length + 1);
                        isSel = SetSelected(chd, selFullName, ref currSubFullName);
                        if (isSel) return true;
                    }
                }
            }
            return isSel;
        }
        /// <summary>
        /// 设定选择的路径
        /// </summary>
        /// <param name="selFullName">指定路径全名</param>
        /// <returns>返回是否查找到结果</returns>
        public bool SetSelected(string selFullName)
        {
            string currSubFullName = selFullName;
            return SetSelected(TreeRoot, selFullName, ref currSubFullName);
        }
        #endregion
        #region 数据处理
        //填充数据
        void FillData()
        {
            AddSubNode("", TreeRoot);
        }
        //添加子节点数据
        void AddSubNode(string parentKey, TreeListViewItemViewModel parentNode)
        {
            if (_DataSource == null) return;
            if (ParentKey == "" || Key == "")
                throw new NotSupportedException("未指定ParentKey或者Key属性值");
            _DataSource.RowFilter = ParentKey + " = '" + parentKey + "'";//获取父节点下的子节点
            //添加子节点到集合中
            foreach (DataRowView rowView in _DataSource)
            {
                TreeListViewItemViewModel node = null;
                bool isDummy = false;//是否使用动态延迟加载
                //创建子节点
                if (!IsUseDummyChild) //不使用延迟加载且为根节点
                {
                    node = new TreeListViewItemViewModel(rowView);
                    node.AfterTreeListItemSelected = TreeListItemSelected;
                    if (parentNode.FullPath == "")
                        node.FullPath = rowView[Key].ToString();
                    else
                        node.FullPath = parentNode.FullPath + "." + rowView[Key];//记录用于定位的完整路径信息
                    //添加到父节点集合
                    parentNode.Children.Add(node);
                    //不使用延迟加载时，添加所有的子项
                    AddSubNode(rowView[Key].ToString(), node);
                }
                else //在主开关使用了延迟加载后，通过字段控制延迟加载可以保证最底层的树不显示延迟加载
                {
                    if (IsDummyChildField == "") //未指定加载字段，则由主开关来确定使用延迟加载
                    {
                        node = new TreeListViewItemViewModel(rowView, true);
                        node.AfterTreeListItemSelected = TreeListItemSelected;
                        node.AfterListItemIsExpanded = AfterNodeIsExpanded;
                        if (parentNode.FullPath == "")
                            node.FullPath = rowView[Key].ToString();
                        else
                            node.FullPath = parentNode.FullPath + "." + rowView[Key];//记录用于定位的完整路径信息
                        //添加到父节点集合
                        parentNode.Children.Add(node);
                    }
                    else
                    {
                        if (rowView[IsDummyChildField] is DBNull)
                            isDummy = true;
                        else
                            isDummy = System.Convert.ToBoolean(rowView[IsDummyChildField]);
                        if (isDummy)
                        {
                            node = new TreeListViewItemViewModel(rowView, true);
                            node.AfterTreeListItemSelected = TreeListItemSelected;
                            node.AfterListItemIsExpanded = AfterNodeIsExpanded;
                            if (parentNode.FullPath == "")
                                node.FullPath = rowView[Key].ToString();
                            else
                                node.FullPath = parentNode.FullPath + "." + rowView[Key];//记录用于定位的完整路径信息
                            //添加到父节点集合
                            parentNode.Children.Add(node);
                        }
                        else //不使用延迟加载
                        {
                            node = new TreeListViewItemViewModel(rowView);
                            node.AfterTreeListItemSelected = TreeListItemSelected;
                            if (parentNode.FullPath == "")
                                node.FullPath = rowView[Key].ToString();
                            else
                                node.FullPath = parentNode.FullPath + "." + rowView[Key];//记录用于定位的完整路径信息
                            //添加到父节点集合
                            parentNode.Children.Add(node);
                            //为了提高性能，默认为是最内层的数据，不会再添加子项
                            //AddSubNode(rowView[Key].ToString(), node);
                        }
                    }
                }

            }
        }
        //展开时加载数据项
        void AfterNodeIsExpanded(TreeListViewItemViewModel node)
        {
            DataRowView dt = node.DataRowView;//当前数据项
            AddSubNode(dt[Key].ToString(), node);
        }

        #endregion
        #region 存储命令
        RelayCommand _SaveRowCommand;
        public RelayCommand SaveRowCommand
        {
            get
            {
                if (_SaveRowCommand == null)
                {
                    _SaveRowCommand = new RelayCommand(SaveRow, CanSaveRow);
                }
                return _SaveRowCommand;
            }
            set
            {
                _SaveRowCommand = value;
            }
        }
        public virtual bool CanSaveRow() //是否能够执行
        {
            bool isChange = false;
            foreach (DataRow row in _DataSource.Table.Rows)
            {
                if (row.RowState != DataRowState.Unchanged)
                {
                    return true;
                }
            }
            return isChange;
        }
        //用于执行重载的行
        public virtual void SaveRow()//命令执行
        {


        }
        #endregion

    }//
}//
