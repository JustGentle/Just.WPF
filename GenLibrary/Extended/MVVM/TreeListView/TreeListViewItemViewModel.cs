using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using GenLibrary.MVVM.Base;
using System.Data;
namespace GenLibrary.MVVM.TreeListView
{
    //定义在项被选择后需要执行的委托
    public delegate void DelAfterSelectedList(TreeListViewItemViewModel currItem);
    //定义在项将要取消选择时需要执行的委托
    public delegate void DelAfterDisSelectedList(TreeListViewItemViewModel currItem);
    //定义在项被展开时延时加载的委托
    public delegate void DelDelayLoadChildrenList(TreeListViewItemViewModel currItem);
    /// <summary>
    /// 树形导航器项数据类视图模式(TreeView的标准类)
    /// </summary>
    public class TreeListViewItemViewModel : ViewModelBase
    {
        #region 定义存储子对象的集合类型
        public class TreeListItems : ObservableCollection<TreeListViewItemViewModel>
        {
            TreeListViewItemViewModel _Paren;//记录父对象
            public TreeListItems(TreeListViewItemViewModel paren)
            {
                _Paren = paren;
            }
            #region 标准添加操作
            /// <summary>
            /// 添加子节点,注意此方法不考虑有虚拟子节点的情况，正常调用可以使用AddOverDummyChild
            /// </summary>
            /// <param name="item">指定子节点对象</param>
            public new void Add(TreeListViewItemViewModel item)
            {
                //如果存在，先删除动态节点
                _Paren.DeleteDummyChildIfExist();
                //检测是否为子节点的最后一个节点
                item.IsNotChildrenLast = false;
                if (_Paren.Children.Count > 0)
                {
                    _Paren.Children[_Paren.Children.Count - 1].IsNotChildrenLast = true;
                }
                item._parent = _Paren;//重新设定父对象                
                base.Add(item);
            }

            #endregion
            //仅用于内部虚拟节点的添加
            internal void AddDummyChild(TreeListViewItemViewModel item)
            {
                item._parent = _Paren;//重新设定父对象                
                base.Add(item);
            }

            #region 标准移除操作
            /// <summary>
            /// 移除指定的子对象
            /// </summary>
            /// <param name="item">指定需要移除的对象</param>
            public new void Remove(TreeListViewItemViewModel item)
            {
                //检测其是否为最后的一个子项
                if (item._parent != null)
                {
                    if (item == item._parent.Children[item._parent.Children.Count - 1])
                    {
                        if (item._parent.Children.Count > 1) //有至少两项
                        {
                            item._parent.Children[item._parent.Children.Count - 2].IsNotChildrenLast = false;
                        }
                    }
                }
                item._parent = null; //设定父对象为空
                item.DataRowView = null;
                base.Remove(item);
            }
            /// <summary>
            /// 从指定位置处移除对象
            /// </summary>
            /// <param name="index">指定位置索引</param>
            protected override void RemoveItem(int index)
            {

                //检测其是否为最后的一个子项
                if (base[index]._parent != null)
                {
                    if (index == base[index]._parent.Children.Count - 1)
                    {
                        if (base[index]._parent.Children.Count > 1) //有至少两项
                        {
                            base[index]._parent.Children[index - 1].IsNotChildrenLast = false;
                        }
                    }


                }
                base[index]._parent = null; //设定父对象为空
                base[index].DataRowView = null;

                base.RemoveItem(index);
            }
            #endregion
        }
        #endregion
        #region 变量定义
        //声明委托变量，外部可以对其设定
        public DelAfterSelectedList AfterTreeListItemSelected;
        public DelAfterDisSelectedList AfterTreeListItemsDiselected;
        public DelDelayLoadChildrenList AfterListItemIsExpanded;
        bool _isExpanded;
        bool _isSelected;
        static readonly TreeListViewItemViewModel DummyChild = new TreeListViewItemViewModel();
        readonly TreeListItems _children;
        TreeListViewItemViewModel _parent; //记录父对象
        #endregion
        #region 构造函数
        //私有构造函数用于创建延迟加载项
        TreeListViewItemViewModel()
        {
        }
        //用于外部继承使用
        public TreeListViewItemViewModel(params string[] data)
        {
            _children = new TreeListItems(this);
            if (data == null) return;
            if (data.Length > 0) Name = data[0];
            if (data.Length > 1) ImagePath = data[1];
        }
        public TreeListViewItemViewModel(bool lazyLoadChildren)
        {
            _children = new TreeListItems(this);
            if (lazyLoadChildren)
                _children.AddDummyChild(DummyChild);

        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="lazyLoadChildren">确定是否延迟加载，=true使用延迟加载</param>
        /// <param name="Del">传递当项被选中时执行的函数</param>
        public TreeListViewItemViewModel(bool lazyLoadChildren, DelAfterSelectedList Del)
        {
            _children = new TreeListItems(this); ;
            if (lazyLoadChildren)
                _children.AddDummyChild(DummyChild);

            AfterTreeListItemSelected = Del;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="lazyLoadChildren">确定是否延迟加载，=true使用延迟加载</param>
        /// <param name="DelSel">传递当项被选中时执行的函数</param>
        /// <param name="DelDisSel">传递当项取消选中时执行的函数</param>
        public TreeListViewItemViewModel(bool lazyLoadChildren, DelAfterSelectedList DelSel, DelAfterDisSelectedList DelDisSel)
        {

            _children = new TreeListItems(this); ;
            if (lazyLoadChildren)
                _children.AddDummyChild(DummyChild);
            AfterTreeListItemSelected = DelSel;
            AfterTreeListItemsDiselected = DelDisSel;
        }
        /// <summary>
        /// 创建树形视图项数据
        /// </summary>
        /// <param name="data">指定树形项数据附加的数据表行数据</param>
        public TreeListViewItemViewModel(DataRowView data)
        {
            _children = new TreeListItems(this);
            _DataRowView = data;

        }
        /// <summary>
        /// 创建树形视图项数据
        /// </summary>
        /// <param name="data">指定树形项数据附加的数据表行数据</param>
        /// <param name="lazyLoadChildren">确定是否延迟加载</param>
        public TreeListViewItemViewModel(DataRowView data, bool lazyLoadChildren)
        {
            _children = new TreeListItems(this);
            _DataRowView = data;
            if (lazyLoadChildren)
                _children.AddDummyChild(DummyChild);

        }
        /// <summary>
        /// 创建树形视图项数据
        /// </summary>
        /// <param name="data">指定树形项数据附加的数据表行数据</param>
        /// <param name="lazyLoadChildren">确定是否延迟加载</param>
        /// <param name="DelSel">传递当项被选中时执行的函数</param>
        public TreeListViewItemViewModel(DataRowView data, bool lazyLoadChildren, DelAfterSelectedList DelSel)
        {
            _children = new TreeListItems(this);
            _DataRowView = data;
            if (lazyLoadChildren)
                _children.AddDummyChild(DummyChild);
            AfterTreeListItemSelected = DelSel;
        }

        #endregion

        #region 子节点操作

        /// <summary>
        ///返回子对象
        /// </summary>
        public TreeListItems Children
        {
            get { return _children; }
        }
        /// <summary>
        /// 确定子对象是否可动态加载
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }
        /// <summary>
        /// 如果有用于延迟加载的动态子节点，则删除动态节点,并触发装载子节点命令,用于添加节点时使用
        /// </summary>
        /// <returns>返回是否有动态子节点，并进行了删除和重新装载操作</returns>
        public bool InitDummyChild()
        {
            //首次使用时，需要删除动态节点
            if (this.HasDummyChild & Children.Contains(DummyChild))
            {
                this.Children.Remove(DummyChild);
                this.LoadChildren();
                return true;
            }
            else
                return false;
        }
        //删除子节点
        internal void DeleteDummyChildIfExist()
        {
            if (this.HasDummyChild & Children.Contains(DummyChild))
            {
                this.Children.Remove(DummyChild);
            }
        }
        /// <summary>
        /// 装载子项
        ///在继承类下可重载此函数，实现具体的装载
        /// </summary>
        protected virtual void LoadChildren()
        {
            if (AfterListItemIsExpanded != null) //若委托不为空，调用委托，从外部装载子节点
                AfterListItemIsExpanded(this);
        }
        //展开所有子节点
        public void ExpandAllChildren()
        {
            this.IsExpanded = true;
            foreach (TreeListViewItemViewModel item in Children)
            {
                item.ExpandAllChildren();
            }
        }
        #endregion
        #region 和TreeView操作绑定的属性
        /// <summary>
        /// 记录从父节点到当前节点的完整路径
        /// </summary>
        string _FullPath = "";
        public string FullPath
        {
            set
            {
                if (_FullPath != value)
                {
                    _FullPath = value;
                    this.OnPropertyChanged(() => this.FullPath);
                }
            }
            get
            {
                return _FullPath;
            }
        }
        /// <summary>
        /// 获取/设置项是否被展开
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged(() => this.IsExpanded); //通知展开命令
                }

                // 展开所有上一级对象
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                // 按需延时装载子项
                if (this.HasDummyChild)
                {
                    this.Children.Remove(DummyChild);
                    this.LoadChildren();
                }
            }
        }
        /// <summary>
        /// 获取/设置项是否被选择
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    this.OnPropertyChanged(() => this.IsSelected);
                    //切换时，一般是将要失去选择的项先激活IsSelected=false；
                    //然后是将要被选择的项激活IsSelected=true；
                    if (_isSelected) //当前项被选择了
                    {
                        if (_parent != null)
                            _parent.SelectedSunItem = this;
                        AfterItemSelected();
                    }
                    else //当前项被取消选择
                    {
                        if (_parent != null)
                            _parent.SelectedSunItem = null;
                        AfterItemDisSelected();
                    }
                }
            }
        }
        /// <summary>
        /// 在项被选择后执行，可以重载
        /// </summary>
        protected virtual void AfterItemSelected()
        {
            if (AfterTreeListItemSelected != null) //调用委托
                AfterTreeListItemSelected(this);
        }
        /// <summary>
        /// 在项被取消选择时激活
        /// </summary>
        protected virtual void AfterItemDisSelected()
        {
            if (AfterTreeListItemsDiselected != null) //调用委托
                AfterTreeListItemsDiselected(this);
        }
        #endregion
        #region 用于访问的属性        
        TreeListViewItemViewModel _SelectedSunItem = null;
        /// <summary>
        /// 返回当前项下面被选择的子项
        /// </summary>
        public TreeListViewItemViewModel SelectedSunItem
        {
            get { return _SelectedSunItem; }
            set
            {
                if (value != _SelectedSunItem)
                    _SelectedSunItem = value;
                this.OnPropertyChanged(() => this.SelectedSunItem);
            }
        }

        //返回或者设定父对象
        public TreeListViewItemViewModel Parent
        {
            get { return _parent; }
        }
        string _ImagePath = "";
        /// <summary>
        /// 获取/设定节点图标
        /// </summary>
        public string ImagePath
        {
            set
            {
                _ImagePath = value;
                this.OnPropertyChanged(() => this.ImagePath);
            }
            get
            {
                return _ImagePath;
            }
        }
        string _Name = "";
        //名称数据
        public string Name
        {
            set
            {
                _Name = value;
                this.OnPropertyChanged(() => this.Name);
            }
            get
            {
                return _Name;
            }
        }
        private int _level = -1;
        /// <summary>
        /// 获取当前节点的级别，如果为0，表示无父节点，相当于根节点，否则每层+1
        /// </summary>
        public int Level
        {
            get
            {
                if (_level == -1)
                {
                    _level = (_parent != null) ? _parent.Level + 1 : 0;
                }
                return _level;
            }
        }
        int _IndexPos = -1;
        /// <summary>
        /// 设置项在所有项中的位置
        /// </summary>
        public int IndexPos
        {
            get
            {
                return _IndexPos;
            }
            set
            {
                _IndexPos = value;
                this.OnPropertyChanged(() => this.IndexPos);
            }
        }
        /// <summary>
        /// 设置项的位置索引
        /// </summary>
        /// <param name="treeList"></param>
        public static void SetIndexPos(TreeListViewItemViewModel treeList)
        {
            int idx = 1;
            SetIndexPos(treeList, ref idx);
        }
        static void SetIndexPos(TreeListViewItemViewModel treeList, ref int idx)
        {
            if (treeList.Children == null) return;
            foreach (TreeListViewItemViewModel sunbItem in treeList.Children)
            {
                if (idx < 1)
                {
                    sunbItem.IndexPos = 1; idx = 1;
                }
                else
                {
                    sunbItem.IndexPos = 0; idx = 0;
                }
                SetIndexPos(sunbItem, ref idx);
            }
        }

        DataRowView _DataRowView = null;
        /// <summary>
        /// 数据行视图
        /// </summary>
        public DataRowView DataRowView
        {
            get
            {
                return _DataRowView;
            }
            set
            {
                _DataRowView = value;
                this.OnPropertyChanged(() => this.DataRowView);
            }
        }
        #endregion
        #region 用于控制节点的连接线的显示
        bool _IsNotChildrenLast = true;
        /// <summary>
        /// 用于确定当前节点是否为父节点下的最后一个子节点
        /// </summary>
        public bool IsNotChildrenLast
        {
            get { return _IsNotChildrenLast; }
            set
            {
                if (_IsNotChildrenLast != value)
                {
                    _IsNotChildrenLast = value;
                    this.OnPropertyChanged(() => this.IsNotChildrenLast);
                }
            }
        }
        bool _IsNotRootFirst = true;
        /// <summary>
        /// 用于确定当前节点是否为根节点下的第一个子节点(根节点为容器，不可见)
        /// </summary>
        public bool IsNotRootFirst
        {
            get { return _IsNotRootFirst; }
            set
            {
                if (_IsNotRootFirst != value)
                {
                    _IsNotRootFirst = value;
                    this.OnPropertyChanged(() => this.IsNotRootFirst);
                }
            }
        }
        #endregion
    }//结束类
}//结束命名空间
