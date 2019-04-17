using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
namespace GenLibrary.GenControls
{
    [System.ComponentModel.DesignTimeVisible(false)] //不在工具箱显示
    public class TreeListViewItem : TreeViewItem
    {

        static TreeListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListViewItem), new FrameworkPropertyMetadata(typeof(TreeListViewItem)));

        }
        public TreeListViewItem()
        {

            //this.PrepareToAdjustFirstColumnWidth();

        }
        #region 自动调整第一列代码
        //准备到调整第一列
        private void PrepareToAdjustFirstColumnWidth()
        {

            this.ItemContainerGenerator.StatusChanged += (o, e) =>
            {

                if (this.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                {

                    if (this.Items.Count > 0)
                    {

                        var item = this.Items[this.Items.Count - 1];

                        TreeListViewItem treeItem = this.ItemContainerGenerator.ContainerFromItem(item) as TreeListViewItem;

                        treeItem.Loaded += (oo, ee) =>
                        {

                            treeItem.AdjustFirstColumnWidth();//调用第一列的调整尺寸信息

                        };

                    }

                }

            };

        }
        //查找行
        private TreeGridViewRowPresenter FindGridRow()
        {

            var rowPresenter = this.Template.FindName("PART_Header", this) as TreeGridViewRowPresenter;

            return rowPresenter;

        }
        //调整第一列宽度
        void AdjustFirstColumnWidth()
        {
            var rowPresenter = this.FindGridRow();
            if (VisualTreeHelper.GetChildrenCount(rowPresenter) <= 0) return;
            //GridViewRowPresenter中的每一个元素表示一列。
            var firstColumn = VisualTreeHelper.GetChild(rowPresenter, 0) as UIElement;
            var desiredWidth = firstColumn.DesiredSize.Width;
            //需要的宽度前，需要加上列的缩进和Expander的宽度。
            var indent = rowPresenter.FirstColumnIndent + rowPresenter.Expander.DesiredSize.Width;
            double firstColumnwidth = indent + firstColumn.DesiredSize.Width + 20;
            if (rowPresenter.Columns == null) return;
            if (rowPresenter.Columns[0].Width < firstColumnwidth)
                rowPresenter.Columns[0].Width = firstColumnwidth;
        }

        #endregion
        /// <summary>
        /// Item's hierarchy in the tree
        /// </summary>
        public int Level
        {
            get
            {
                if (_level == -1)
                {
                    TreeListViewItem parent = ItemsControl.ItemsControlFromItemContainer(this) as TreeListViewItem;
                    _level = (parent != null) ? parent.Level + 1 : 0;
                }
                return _level;
            }
        }


        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeListViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        private int _level = -1;

        public int IndexPos
        {
            set
            {

                this.SetValue(IndexPosProperty, value);

            }
            get
            {
                return (int)this.GetValue(IndexPosProperty);

            }
        }
        public static readonly DependencyProperty IndexPosProperty =
  DependencyProperty.Register("IndexPos", typeof(int), typeof(TreeListViewItem), new FrameworkPropertyMetadata(0));
    }//结束类
}//结束命名空间
