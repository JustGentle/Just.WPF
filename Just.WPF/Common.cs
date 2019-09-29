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
            config.AddJsonFile(System.Configuration.ConfigurationManager.AppSettings["autofac"]);
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

            var tab = GetTabItem(code);
            if (tab == null)
            {
                var view = CreateView(code, title);
                if (view == null) return;
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
                {

                    var view = CreateView(code, title);
                    if (view == null) return;
                    tab.Content = CreateView(code, title);
                }
            }
        }
        private UserControl CreateView(string className, string title)
        {
            UserControl view = null;
            try
            {
                view = Container.ResolveNamed<IDependency>(className) as UserControl;
                if (view == null)
                {
                    Logger.Warn($"【{title}】初始化失败");
                    MessageWin.Warn($"【{title}】初始化失败");
                    return null;
                }
                view.Unloaded += ChildView_Unloaded;
            }
            catch (Exception ex)
            {
                Logger.Error($"【{title}】初始化错误", ex);
                MessageWin.Error($"【{title}】初始化错误");
                return null;
            }
            return view;
        }

        private void ChildView_Unloaded(object sender, RoutedEventArgs e)
        {
            (sender as IChildView)?.WriteSettings();
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
            CloseWindow(item);
        }
        private void CloseWindow(TabItem item)
        {
            if (item == null) return;
            if (item.Content is ContentControl content
                && content.Content is IChildView writer)
            {
                writer.WriteSettings();
            }
            this.tbContent.Items.Remove(item);
        }

        public void CloseAll()
        {
            while (tbContent.HasItems)
            {
                var item = tbContent.Items[0] as TabItem;
                if(item == null)
                {
                    this.tbContent.Items.RemoveAt(0);
                }
                else
                {
                    CloseWindow(item);
                }
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
