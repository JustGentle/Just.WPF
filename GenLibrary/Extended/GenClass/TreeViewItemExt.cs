using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System;
using System.Windows.Controls.Primitives;
namespace GenLibrary.GenClass
{
	/// <summary>
	///用于实现TreeView控件的项被整行选择偏移计算
	/// </summary>
	public static class TreeViewItemExt
	{
		/// <summary>
		/// 返回指定 <see cref="System.Windows.Controls.TreeViewItem"/> 的深度。
		/// </summary>
		/// <param name="item">要获取深度的 <see cref="System.Windows.Controls.TreeViewItem"/> 对象。</param>
		/// <returns><see cref="System.Windows.Controls.TreeViewItem"/> 所在的深度。</returns>
		public static int GetDepth(this TreeViewItem item)
		{
			int depth = 0;
			while ((item = item.GetAncestor<TreeViewItem>()) != null)
			{
				depth++;
			}
			return depth;
		}
	}
    internal static class VisualTreeEx
    {
        /// <summary>
        /// 返回指定对象的特定类型的祖先。
        /// </summary>
        /// <typeparam name="T">要获取的祖先的类型。</typeparam>
        /// <param name="source">获取的祖先，如果不存在则为 <c>null</c>。</param>
        /// <returns>获取的祖先对象。</returns>
        public static T GetAncestor<T>(this DependencyObject source)
            where T : DependencyObject
        {
            do
            {
                source = VisualTreeHelper.GetParent(source);
            } while (source != null && !(source is T));
            return source as T;
        }
    }
    /// <summary>
    /// 计算 <see cref="System.Windows.Controls.TreeViewItem"/> 的缩进的转换器。
    /// </summary>
    [ValueConversion(typeof(TreeViewItem), typeof(Thickness))]
    public sealed class IndentConverter : IValueConverter
    {
        /// <summary>
        /// 获取或设置缩进的像素个数。
        /// </summary>
        public double Indent { get; set; }
        /// <summary>
        /// 获取或设置初始的左边距。
        /// </summary>
        public double MarginLeft { get; set; }
        /// <summary>
        /// 转换值。
        /// </summary>
        /// <param name="value">绑定源生成的值。</param>
        /// <param name="targetType">绑定目标属性的类型。</param>
        /// <param name="parameter">要使用的转换器参数。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <returns>转换后的值。如果该方法返回 <c>null</c>，则使用有效的 <c>null</c> 值。</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TreeViewItem item = value as TreeViewItem;
            if (item == null)
            {
                return new Thickness(0);
            }
            return new Thickness(this.MarginLeft + this.Indent * item.GetDepth(), 0, 0, 0);
        }
        /// <summary>
        /// 转换值。
        /// </summary>
        /// <param name="value">绑定目标生成的值。</param>
        /// <param name="targetType">要转换到的类型。</param>
        /// <param name="parameter">要使用的转换器参数。</param>
        /// <param name="culture">要用在转换器中的区域性。</param>
        /// <returns>转换后的值。如果该方法返回 <c>null</c>，则使用有效的 <c>null</c> 值。</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    //展开线转换器
    public  class TreeViewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TreeViewItem item = (TreeViewItem)value;
            //ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
            //return ic.ItemContainerGenerator.IndexFromContainer(item) == ic.Items.Count - 1;
            //if(item.Header is MVVM.TreeListView.TreeListViewItemViewModel)
            //{
            //    MVVM.TreeListView.TreeListViewItemViewModel header = item.Header as MVVM.TreeListView.TreeListViewItemViewModel;
            //    if (header.Parent != null)
            //    {
            //        return header.Parent.Children.Count * 24 / 2;
            //    }
            //}
            return item.ActualHeight / 2; 

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }
    //展开线行为
    public class TVIExtender
    {
        private TreeViewItem _item;

        public static DependencyProperty UseExtenderProperty =
          DependencyProperty.RegisterAttached("UseExtender", typeof(bool), typeof(TVIExtender),
                                              new PropertyMetadata(false, new PropertyChangedCallback(OnChangedUseExtender)));

        public static bool GetUseExtender(DependencyObject sender)
        {
            return (bool)sender.GetValue(UseExtenderProperty);
        }
        public static void SetUseExtender(DependencyObject sender, bool useExtender)
        {
            sender.SetValue(UseExtenderProperty, useExtender);
        }

        private static void OnChangedUseExtender(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (null != item)
            {
                if ((bool)e.NewValue)
                {
                    if (item.ReadLocalValue(ItemExtenderProperty) == DependencyProperty.UnsetValue)
                    {
                        TVIExtender extender = new TVIExtender(item);
                        item.SetValue(ItemExtenderProperty, extender);
                    }
                }
                else
                {
                    if (item.ReadLocalValue(ItemExtenderProperty) != DependencyProperty.UnsetValue)
                    {
                        TVIExtender extender = (TVIExtender)item.ReadLocalValue(ItemExtenderProperty);
                        extender.Detach();
                        item.SetValue(ItemExtenderProperty, DependencyProperty.UnsetValue);
                    }
                }
            }
        }

        public static DependencyProperty ItemExtenderProperty =
          DependencyProperty.RegisterAttached("ItemExtender", typeof(TVIExtender), typeof(TVIExtender));

        public static DependencyProperty IsLastOneProperty =
          DependencyProperty.RegisterAttached("IsLastOne", typeof(bool), typeof(TVIExtender));

        public static bool GetIsLastOne(DependencyObject sender)
        {
            return (bool)sender.GetValue(IsLastOneProperty);
        }
        public static void SetIsLastOne(DependencyObject sender, bool isLastOne)
        {
            sender.SetValue(IsLastOneProperty, isLastOne);
        }
        #region 检查是否为第一项
        public static DependencyProperty IsFirstOneProperty =
          DependencyProperty.RegisterAttached("IsFirstOne", typeof(bool), typeof(TVIExtender));

        public static bool GetIsFirstOne(DependencyObject sender)
        {
            return (bool)sender.GetValue(IsFirstOneProperty);
        }
        public static void SetIsFirstOne(DependencyObject sender, bool IsFirstOne)
        {
            sender.SetValue(IsFirstOneProperty, IsFirstOne);
        }
        #endregion
        public TVIExtender(TreeViewItem item)
        {
            _item = item;

            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(_item);
            ic.ItemContainerGenerator.ItemsChanged += OnItemsChangedItemContainerGenerator;

            _item.SetValue(IsLastOneProperty,
                     ic.ItemContainerGenerator.IndexFromContainer(_item) == ic.Items.Count - 1);
            //确定第一个节点
            ItemsControl icPar = ItemsControl.ItemsControlFromItemContainer(ic);
            _item.SetValue(IsFirstOneProperty,
                     (icPar == null) && ic.ItemContainerGenerator.IndexFromContainer(_item) == 0);
        }

        void OnItemsChangedItemContainerGenerator(object sender, ItemsChangedEventArgs e)
        {
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(_item);

            if (null != ic)
                _item.SetValue(IsLastOneProperty,
                               ic.ItemContainerGenerator.IndexFromContainer(_item) == ic.Items.Count - 1);
        }

        private void Detach()
        {
            if (_item != null)
            {
                ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(_item);
                if (ic == null)
                {
                    _item.ItemContainerGenerator.ItemsChanged -= OnItemsChangedItemContainerGenerator;
                }
                else
                    ic.ItemContainerGenerator.ItemsChanged -= OnItemsChangedItemContainerGenerator;

                _item = null;
            }
        }
    }

}//
