using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Just.Base.Views
{
    /// <summary>
    /// NotifyWin.xaml 的交互逻辑
    /// </summary>
    public partial class NotifyWin : Window
    {
        public static void Info(string msg, string title = "提示")
        {
            if (NotifyTopOver(MainWindowVM.Instance.MainWindow)) return;
            new NotifyWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                Owner = MainWindowVM.Instance.MainWindow
            }.Show();
        }
        public static void Warn(string msg, string title = "警告")
        {
            if (NotifyTopOver(MainWindowVM.Instance.MainWindow)) return;
            new NotifyWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("OrangeBrush"),
                Owner = MainWindowVM.Instance.MainWindow
            }.Show();
        }
        public static void Error(string msg, string title = "错误")
        {
            if (NotifyTopOver(MainWindowVM.Instance.MainWindow)) return;
            new NotifyWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("RedBrush"),
                Owner = MainWindowVM.Instance.MainWindow
            }.Show();
        }
        public static List<double> NotifyTops { get; } = new List<double>();
        private static bool NotifyTopOver(Window owner)
        {
            //提示信息超出屏幕时不显示
            var top = (NotifyTops.Any() ? NotifyTops.Max() : 0);
            if (owner.Top + owner.ActualHeight < top) return true;
            return false;
        }

        private double notifyTop;

        public NotifyWin()
        {
            InitializeComponent();
            LayoutRoot.MaxWidth = SystemParameters.WorkArea.Width;
            LayoutRoot.MaxHeight = SystemParameters.WorkArea.Height;
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(NotifyWin), new PropertyMetadata(string.Empty));



        private void NotifyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            notifyTop = (NotifyTops.Any() ? NotifyTops.Max() : 0) + this.ActualHeight;
            NotifyTops.Add(notifyTop);
            if (this.Owner != null)
            {
                switch (Owner.WindowState)
                {
                    case WindowState.Normal:
                        this.Left = this.Owner.Left + this.Owner.ActualWidth - this.ActualWidth;
                        this.Top = this.Owner.Top + this.Owner.ActualHeight - notifyTop;
                        break;
                    case WindowState.Minimized:
                    case WindowState.Maximized:
                    default:
                        this.Left = SystemParameters.WorkArea.Width - this.ActualWidth;
                        this.Top = SystemParameters.WorkArea.Height - notifyTop;
                        break;
                }
            }
        }
        private void SBFade_Completed(object sender, System.EventArgs e)
        {
            this.Close();
            NotifyTops.Remove(notifyTop);
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
            e.Handled = true;
        }
    }
}
