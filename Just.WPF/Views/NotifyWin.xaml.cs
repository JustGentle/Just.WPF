using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Just.WPF.Views
{
    /// <summary>
    /// NotifyWin.xaml 的交互逻辑
    /// </summary>
    public partial class NotifyWin : Window
    {
        public static void Info(string msg, string title = "提示")
        {
            new NotifyWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                Owner = MainWindow.Instance
            }.Show();
        }
        public static void Warn(string msg, string title = "警告")
        {
            new NotifyWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("OrangeBrush"),
                Owner = MainWindow.Instance
            }.Show();
        }
        public static void Error(string msg, string title = "错误")
        {
            new NotifyWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("RedBrush"),
                Owner = MainWindow.Instance
            }.Show();
        }
        public static List<double> NotifyTops { get; } = new List<double>();

        private double notifyTop;

        public NotifyWin()
        {
            InitializeComponent();
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

    }
}
