using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Reflection;
namespace GenLibrary.GenControls
{
    public class TreeListView : TreeView
    {

        static TreeListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListView), new FrameworkPropertyMetadata(typeof(TreeListView)));

            //ItemsPanelTemplate template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            //template.Seal();
            //ItemsControl.ItemsPanelProperty.OverrideMetadata(typeof(TreeListView), new FrameworkPropertyMetadata(template)); 
            //取消虚拟化
            //VirtualizingStackPanel.IsVirtualizingProperty.OverrideMetadata(typeof(TreeListView), new FrameworkPropertyMetadata(true));
        }       
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
        }

        #region Public Properties

        /// <summary> GridViewColumn List</summary>
        public GridViewColumnCollection Columns
        {
            get
            {
                if (_columns == null)
                {
                    _columns = new GridViewColumnCollection();
                }

                return _columns;
            }
        }

        private GridViewColumnCollection _columns;



        public int ItemHeight
        {
            get { return (int)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeight", typeof(int), typeof(TreeListView), new PropertyMetadata(0));

        #endregion
    }//结束类
}//结束命名空间
