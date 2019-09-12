using Just.Base;
using Just.Base.Views;
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
using System.Windows.Threading;

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

        private readonly MainWindowVM _vm;

        public MainWindow()
        {
            InitializeComponent();
            _Instance = this;

            DependencyResolverInitialize();
            _vm = MainWindowVM.Instance;
            this.DataContext = _vm;
        }

        /// <summary>
        /// 窗体加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.MainWindow = this;
            CreateMenu();
            foreach (var item in _vm.StartOn)
            {
                ShowWindow(item.Header, item.ClassName);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (tbContent.HasItems)
            {
                if (!MessageWin.Confirm("确定退出?") ?? true)
                {
                    e.Cancel = true;
                    return;
                }
                CloseAll();
            }
            MainWindowVM.SaveSetting();
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

        #region 主菜单

        /// <summary>
        /// 获取菜单
        /// </summary>
        public void CreateMenu()
        {
            _vm.LoadMainMenu();
            var topMenus = _vm.MainMenu.Where(menu => menu.Parent == null);
            foreach (var menu in topMenus)
            {
                TreeViewItem item = new TreeViewItem
                {
                    Header = menu.Header,
                    Tag = menu.ClassName,
                    Name = "tv" + menu.Id,
                    Visibility = menu.Visible ? Visibility.Visible : Visibility.Collapsed
                };
                var subMenus = _vm.MainMenu.Where(m => m.Parent == menu.Id);
                CreateNode(subMenus, item);
                item.IsExpanded = true;//主菜单默认展开
                item.MouseLeftButtonUp += Node_MouseUp;
                tvMenu.Items.Add(item);
            }
        }

        /// <summary>
        /// 获取子菜单
        /// </summary>
        /// <param name="data"></param>
        /// <param name="item"></param>
        private void CreateNode(IEnumerable<MenuNode> data, TreeViewItem item)
        {
            if (data == null) return;
            foreach (var sub in data)
            {
                TreeViewItem node = new TreeViewItem
                {
                    Header = sub.Header,
                    Tag = sub.ClassName,
                    Name = "tv" + sub.Id,
                    Visibility = sub.Visible ? Visibility.Visible : Visibility.Collapsed
                };
                node.MouseLeftButtonUp += Node_MouseUp;
                item.Items.Add(node);
                CreateNode(null, node);
            }
        }

        private TreeViewItem GetNode(string tag, ItemsControl parent = null)
        {
            parent = parent ?? this.tvMenu;
            foreach (TreeViewItem item in parent.Items)
            {
                if (item.Tag?.ToString() == tag)
                    return item;
                if (item.HasItems)
                {
                    var node = GetNode(tag, item);
                    if (node != null)
                        return node;
                }
            }
            return null;
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

        /// <summary>
        /// 子窗口选择事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (!(e.AddedItems[0] is TabItem item)) return;
                var node = GetNode(item.Tag?.ToString());
                if(node != null) node.IsSelected = true;
            }
            else if (e.RemovedItems.Count > 0)
            {
                if (!(e.RemovedItems[0] is TabItem item)) return;
                var node = GetNode(item.Tag?.ToString());
                if (node != null) node.IsSelected = false;
            }
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
            MessageWin.Info(this.Title, "关于");
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
            if (MessageWin.Confirm("确定要退出登录?") != true)
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
