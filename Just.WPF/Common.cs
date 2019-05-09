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
        #region Dispatcher
        public static void DispatcherInvoke(Action action)
        {
            Instance.Dispatcher.Invoke(action);
        }
        public static TResult DispatcherInvoke<TResult>(Func<TResult> func)
        {
            return Instance.Dispatcher.Invoke(func);
        }
        #endregion

        #region Setting
        private static Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        public static string ReadSetting(string key, string defaultValue = null)
        {
            var value = cfg.AppSettings.Settings[key]?.Value;
            if (string.IsNullOrEmpty(value))
                value = defaultValue;
            return value;
        }
        public static void WriteSetting(string key, string value)
        {
            var setting = cfg.AppSettings.Settings[key];
            if (setting == null)
                cfg.AppSettings.Settings.Add(key, value);
            else
                setting.Value = value;
        }
        public static T ReadSetting<T>(string key, T defaultValue = default(T))
        {
            var value = cfg.AppSettings.Settings[key]?.Value;
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        public static void WriteSetting<T>(string key, T value)
        {
            var setting = cfg.AppSettings.Settings[key];
            if (setting == null)
                cfg.AppSettings.Settings.Add(key, value?.ToString());
            else
                setting.Value = value?.ToString();
        }
        public static void SaveSetting()
        {
            cfg.Save();
        }
        #endregion

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
                    && content.Content is IWriteSettings writer)
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

        #region StatusBar
        /// <summary>
        /// 显示状态文本
        /// </summary>
        /// <param name="text"></param>
        public void ShowStatus(string text = "就绪", bool isProcess = false, int process = 0)
        {
            _vm.StatusText = text;
            _vm.IsShowStatusProcess = isProcess;
            _vm.StatusProcess = process;
        }
        #endregion
    }
}
