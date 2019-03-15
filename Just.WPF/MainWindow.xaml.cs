using Just.WPF.SubWindows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Just.WPF
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 实例化
        private static MainWindow _Instance;
        //主窗口单例
        public static MainWindow Instance
        {
            get
            {
                _Instance = _Instance ?? new MainWindow();
                return _Instance;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _Instance = this;
        }

        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GetMenu();
            var defaultMenu = menus.FirstOrDefault(menu => menu.ClassName == nameof(RevCleanerCtrl));
            if (defaultMenu != null) ShowWindow(defaultMenu.Header, defaultMenu.ClassName);
        }
        #endregion

        #region 窗口控制
        /// <summary>
        /// 最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinMenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        /// <summary>
        /// 最大化/欢迎
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WinStateMenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SwitchWindowState();
        }
        private void SwitchWindowState()
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseMenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        /// <summary>
        /// 拖动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragMove_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
                SwitchWindowState();//双击切换窗口最大化
            else
                this.DragMove();//拖拽移动窗口
            e.Handled = true;
        }
        /// <summary>
        /// 状态处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowStyle != WindowStyle.None) return;

            //无边框时最大化会全屏覆盖任务栏,需要做处理
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowStyle = WindowStyle.None;
            }
        }
        #endregion

        #region 主窗口公共方法
        /// <summary>
        /// 显示子窗口
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <param name="code">窗口权限代码(后缀为类名)</param>
        public void ShowWindow(string title, string code, bool isOpenRecord = false, params object[] args)
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
                if (isOpenRecord)
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

        #region 主菜单
        private List<dynamic> menus = new List<dynamic>
        {
            new { Parent = default(int?), Id = 0, Header = "iOffice10", ClassName = default(string) },
            new { Parent = (int?)0, Id = 1, Header = "补丁文件清理", ClassName = nameof(RevCleanerCtrl) }
        };

        /// <summary>
        /// 获取菜单
        /// </summary>
        public void GetMenu()
        {
            var topMenus = menus.Where(menu => menu.Parent == null);
            foreach (var menu in topMenus)
            {
                TreeViewItem item = new TreeViewItem
                {
                    Header = menu.Header,
                    Tag = menu.ClassName,
                    Name = "tv_" + menu.Id
                };
                var subMenus = menus.Where(m => m.Parent == menu.Id);
                GetNode(subMenus, item);
                item.IsExpanded = true;//主菜单默认展开
                tvMenu.Items.Add(item);
            }
        }

        /// <summary>
        /// 获取子菜单
        /// </summary>
        /// <param name="data"></param>
        /// <param name="item"></param>
        private void GetNode(IEnumerable<dynamic> data, TreeViewItem item)
        {
            if (data == null) return;
            foreach (var sub in data)
            {
                TreeViewItem node = new TreeViewItem
                {
                    Header = sub.Header,
                    Tag = sub.ClassName,
                    Name = "tv_" + sub.Id
                };
                node.MouseLeftButtonUp += Node_MouseUp;
                item.Items.Add(node);
                GetNode(null, node);
            }
        }

        /// <summary>
        /// 树形控件选择事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Node_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (!(sender is TreeViewItem tv)) return;
            if (tv.HasItems) return;
            ShowWindow(tv.Header?.ToString(), tv.Tag?.ToString());
        }

        #endregion

        #region 顶部菜单
        /// <summary>
        /// 关于
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AboutMenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(this.Title, "关于", MessageBoxButton.OK, MessageBoxImage.Information);
            e.Handled = true;
        }

        /// <summary>
        /// 用户弹出菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            UserPopup.IsOpen = !UserPopup.IsOpen;
        }

        private bool CloseWithoutAsk { get; set; } = false;
        /// <summary>
        /// 退出登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogoutMenuItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("确定要退出登录?", "提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;
            //string output = HttpHelper.GetUrl("/Auth/LoginOut", null, UserInfo.Token);
            //this.CloseWithoutAsk = true;

            //var frmLogin = new Login(UserInfo.Account, string.Empty);
            //frmLogin.Show();
            //UserInfo.UserID = 0;
            //UserInfo.PassWord = "";
            //UserInfo.UserMenu = null;
            //UserInfo.Account = UserInfo.UserName = UserInfo.DepartmentName = UserInfo.Token = string.Empty;
            this.Close();
        }
        #endregion
    }
}
