using Autofac;
using Autofac.Configuration;
using Just.Base;
using Just.Base.Views;
using Microsoft.Extensions.Configuration;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Just.WPF
{
    public partial class MainWindow
    {
        #region Autofac
        private IContainer Container { get; set; }
        private IContainer DependencyResolverInitialize()
        {
            var config = new ConfigurationBuilder();
            config.AddJsonFile("autofac.json");
            var module = new ConfigurationModule(config.Build());
            var builder = new ContainerBuilder();
            builder.RegisterModule(module);
            Container = builder.Build();
            return Container;
        }
        #endregion

        #region Window
        /// <summary>
        /// 显示子窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="code">窗口权限代码(后缀为类名)</param>
        public void ShowWindow(string title, string code, bool isReOpen = false)
        {
            if (code == null || code == string.Empty)
                return;
            string className = code;
            UserControl view = null;
            try
            {
                view = Container.ResolveNamed<IDependency>(className) as UserControl;
                if (view == null)
                {
                    MessageWin.Warn($"【{title}】初始化失败");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"【{title}】初始化错误", ex);
                MessageWin.Error($"【{title}】初始化错误");
                return;
            }

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
                        Content = view
                    }
                };
                tbContent.Items.Add(tab);
            }
            else
            {
                tab.IsSelected = true;
                if (isReOpen)
                    tab.Content = view;
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
                    && content.Content is IChildView writer)
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
