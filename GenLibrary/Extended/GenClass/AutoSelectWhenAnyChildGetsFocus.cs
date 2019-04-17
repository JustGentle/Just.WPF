using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
namespace GenLibrary.GenClass
{
    /// <summary>
    /// 用于当容器控件中的子元素聚焦时，该容器的项将被选择
    /// </summary>
    public class AutoSelectWhenAnyChildGetsFocus
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(AutoSelectWhenAnyChildGetsFocus),
            new UIPropertyMetadata(false, Enabled_Changed));

        public static bool GetEnabled(DependencyObject obj) { return (bool)obj.GetValue(EnabledProperty); }
        public static void SetEnabled(DependencyObject obj, bool value) { obj.SetValue(EnabledProperty, value); }

        private static void Enabled_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var attachEvents = (bool)e.NewValue;
            var targetUiElement = (UIElement)sender;

            if (attachEvents)
                targetUiElement.IsKeyboardFocusWithinChanged += TargetUiElement_IsKeyboardFocusWithinChanged;
            else
                targetUiElement.IsKeyboardFocusWithinChanged -= TargetUiElement_IsKeyboardFocusWithinChanged;
        }

        static void TargetUiElement_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var targetUiElement = (UIElement)sender;
            if (targetUiElement is GenControls.TreeListViewItem) //让其支持TreeListView
            {
                GenControls.TreeListViewItem trItem = targetUiElement as GenControls.TreeListViewItem;
                if (trItem.Header is MVVM.TreeListView.TreeListViewItemViewModel)
                {
                    MVVM.TreeListView.TreeListViewItemViewModel header = trItem.Header as MVVM.TreeListView.TreeListViewItemViewModel;
                    header.IsSelected = true;
                }
            }
            else
            {
                ListBox.SetIsSelected(targetUiElement, true); //设置选择，可用于datagrid/treeview等其他类型，仅仅适合于单选

            }

        }

    }

}
