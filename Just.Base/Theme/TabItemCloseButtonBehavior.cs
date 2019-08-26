using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Just.Base.Theme
{
    public class TabItemCloseButtonBehavior
    {
        public static readonly DependencyProperty IsTabItemCloseButtonProperty = DependencyProperty.RegisterAttached(
             "IsTabItemCloseButton",
             typeof(bool),
             typeof(TabItemCloseButtonBehavior),
             new UIPropertyMetadata(false, IsTabItemCloseButton_Changed));
        public static bool GetIsTabItemCloseButton(DependencyObject obj) { return (bool)obj.GetValue(IsTabItemCloseButtonProperty); }
        public static void SetIsTabItemCloseButton(DependencyObject obj, bool value) { obj.SetValue(IsTabItemCloseButtonProperty, value); }
        private static void IsTabItemCloseButton_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var attachEvents = (bool)e.NewValue;
            var targetUiElement = (Button)sender;

            if (attachEvents)
                targetUiElement.Click += TargetUiElement_Click;
            else
                targetUiElement.Click -= TargetUiElement_Click;
        }

        private static void TargetUiElement_Click(object sender, RoutedEventArgs e)
        {
            var targetUiElement = (Button)sender;
            DependencyObject parentTabItem =
            VisualTreeHelperEx.FindAncestorByType(targetUiElement, typeof(TabItem), true);
            if (parentTabItem != null)
            {
                var tabItem = parentTabItem as TabItem;
                DependencyObject parentTabControl = VisualTreeHelperEx.FindAncestorByType(tabItem, typeof(TabControl), true);
                if (parentTabControl is TabControl tabControl)
                {
                    tabControl.Items.Remove(tabItem);
                }
            }
        }
    }//
}//
