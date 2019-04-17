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
    public class AutoSelectTreeListItemWhenChildIsFocus
    {
        public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
           "Enabled",
           typeof(bool),
           typeof(AutoSelectTreeListItemWhenChildIsFocus),
           new UIPropertyMetadata(false, Enabled_Changed));

        public static bool GetEnabled(DependencyObject obj) { return (bool)obj.GetValue(EnabledProperty); }
        public static void SetEnabled(DependencyObject obj, bool value) { obj.SetValue(EnabledProperty, value); }
        private static void Enabled_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var attachEvents = (bool)e.NewValue;
            var targetUiElement = (UIElement)sender;

            if (attachEvents)
                targetUiElement.GotFocus += TargetUiElement_GotFocus;
            else
                targetUiElement.GotFocus -= TargetUiElement_GotFocus;
        }

        private static void TargetUiElement_GotFocus(object sender, RoutedEventArgs e)
        {
            if(sender is TextBox)
            {
                TextBox tx = sender as TextBox;
                object parent = FindAncestorByType(tx,typeof(GenLibrary.GenControls.TreeListViewItem), false);
                if(parent is GenLibrary.GenControls.TreeListViewItem)
                {
                    GenLibrary.GenControls.TreeListViewItem item = parent as GenLibrary.GenControls.TreeListViewItem;
                    if(item.Header is MVVM.TreeListView.TreeListViewItemViewModel)
                    {
                        MVVM.TreeListView.TreeListViewItemViewModel header = item.Header as MVVM.TreeListView.TreeListViewItemViewModel;
                        header.IsSelected = true;

                    }
                }
            }
        }
        public static DependencyObject FindAncestorByType(DependencyObject element, Type type, bool specificTypeOnly)
        {
            if (element == null)
                return null;

            if (specificTypeOnly ? (element.GetType() == type)
                : (element.GetType() == type) || (element.GetType().IsSubclassOf(type)))
                return element;

            return FindAncestorByType(VisualTreeHelper.GetParent(element), type, specificTypeOnly);
        }
    }//
}//
