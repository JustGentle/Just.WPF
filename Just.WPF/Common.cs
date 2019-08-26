using Just.Base.Views;
using Just.WPF.Views;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Just.WPF
{
    public partial class MainWindow
    {
        #region Window
        /// <summary>
        /// 显示子窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="code">窗口权限代码(后缀为类名)</param>
        public void ShowWindow(string title, string code, bool isReOpen = false, params object[] args)
        {
            if (code == null || code == string.Empty)
                return;
            string className = code;
            //取短名称与类名匹配的用户控件
            Type type = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => typeof(UserControl).IsAssignableFrom(t) && t.Name.Equals(className, StringComparison.CurrentCultureIgnoreCase));
            //调试用
            type = type ?? Assembly.GetExecutingAssembly().GetType("...");

            var tab = GetTabItem(code);
            if (tab == null)
            {
                tab = new TabItem
                {
                    Tag = code,
                    Header = title,
                    IsSelected = true,
                    Content = new ContentControl
                    {
                        Margin = new Thickness(10),
                        Content = (Activator.CreateInstance(type, args) as UserControl)
                    }
                };
                tbContent.Items.Add(tab);
            }
            else
            {
                tab.IsSelected = true;
                if (isReOpen)
                    tab.Content = (Activator.CreateInstance(type, args) as UserControl);
            }
        }
        /// <summary>
        /// 关闭子窗口
        /// </summary>
        /// <param name="uc"></param>
        public void CloseWindow(UserControl uc)
        {
            var className = uc?.GetType().Name;
            CloseWindow(className);
        }
        /// <summary>
        /// 关闭子窗口
        /// </summary>
        /// <param name="code"></param>
        public void CloseWindow(string code)
        {
            var item = GetTabItem(code);
            if (item == null) return;
            this.tbContent.Items.Remove(item);
        }

        public void CloseAll()
        {
            while (tbContent.HasItems)
            {
                //保存设置
                if(tbContent.Items[0] is TabItem item 
                    && item.Content is ContentControl content 
                    && content.Content is IChildViews writer)
                {
                    writer.WriteSettings();
                }
                this.tbContent.Items.RemoveAt(0);
            }
        }

        private TabItem GetTabItem(string code, bool exact = false)
        {
            foreach (TabItem item in tbContent.Items)
            {
                if (item.Tag?.ToString() == code)
                    return item;
                if (item.Tag?.ToString().EndsWith(code) ?? false && !exact)
                    return item;
            }
            return null;
        }
        #endregion
    }
}
