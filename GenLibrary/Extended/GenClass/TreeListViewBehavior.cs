using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace GenLibrary.GenClass
{
    public class TreeListViewBehavior
    {
        #region 设定第一个节点的上连接点不可见性
        public static readonly DependencyProperty IsRootFirstCheckedProperty = DependencyProperty.RegisterAttached(
          "IsRootFirstChecked",
          typeof(bool),
          typeof(TreeListViewBehavior),
          new UIPropertyMetadata(false, IsRootFirstChecked_Changed));
        public static bool GetIsRootFirstChecked(DependencyObject obj)
        { return (bool)obj.GetValue(IsRootFirstCheckedProperty); }
        public static void SetIsRootFirstChecked(DependencyObject obj, bool value)
        { obj.SetValue(IsRootFirstCheckedProperty, value); }
        private static void IsRootFirstChecked_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var attachEvents = (bool)e.NewValue;
            var targetUiElement = (UIElement)sender;
            if (targetUiElement is GenControls.TreeListView)
            {
                GenControls.TreeListView item = targetUiElement as GenControls.TreeListView;
                if (attachEvents)
                    item.DataContextChanged += Item_DataContextChanged;
                else
                    item.DataContextChanged -= Item_DataContextChanged;

            }

        }

        private static void Item_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is GenControls.TreeListView)
            {
                GenControls.TreeListView item = sender as GenControls.TreeListView;
                if (item.DataContext != null)
                {
                    if (item.DataContext is MVVM.TreeListView.TreeListViewItemViewModel)
                    {
                        MVVM.TreeListView.TreeListViewItemViewModel data = item.DataContext as MVVM.TreeListView.TreeListViewItemViewModel;
                        if (data.Children.Count > 0)
                        {
                            data.Children[0].IsNotRootFirst = false;
                        }
                    }
                }
            }

        }
        #endregion
    }//
}//
