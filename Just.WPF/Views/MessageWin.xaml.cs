using System.Windows;
using System.Windows.Media;

namespace Just.WPF.Views
{
    /// <summary>
    /// MessageWin.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWin : Window
    {
        public static void Info(string msg, string title = "提示")
        {
            new MessageWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                Owner = MainWindow.Instance
            }.ShowDialog();
        }
        public static void Warn(string msg, string title = "警告")
        {
            new MessageWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("OrangeBrush"),
                Owner = MainWindow.Instance
            }.ShowDialog();
        }
        public static void Error(string msg, string title = "错误")
        {
            new MessageWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("RedBrush"),
                Owner = MainWindow.Instance
            }.ShowDialog();
        }
        public static bool? Confirm(string msg, string title = "确认")
        {
            return new MessageWin
            {
                IsConfirm = true,
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                Owner = MainWindow.Instance
            }.ShowDialog();
        }

        public MessageWin()
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
            DependencyProperty.Register("Message", typeof(string), typeof(MessageWin), new PropertyMetadata(string.Empty));



        public bool IsConfirm
        {
            get { return (bool)GetValue(IsConfirmProperty); }
            set { SetValue(IsConfirmProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsConfirm.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsConfirmProperty =
            DependencyProperty.Register("IsConfirm", typeof(bool), typeof(MessageWin), new PropertyMetadata(false));

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
