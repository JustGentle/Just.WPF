using ICSharpCode.AvalonEdit.Document;
using System;
using System.Windows;
using System.Windows.Media;

namespace Just.Base.Views
{
    /// <summary>
    /// MessageWin.xaml 的交互逻辑
    /// </summary>
    public partial class MessageWin : Window
    {
        public MessageWin()
        {
            InitializeComponent();
            LayoutRoot.MaxWidth = SystemParameters.WorkArea.Width;
            LayoutRoot.MaxHeight = SystemParameters.WorkArea.Height;
            codeEditor.MaxHeight = SystemParameters.WorkArea.Height - 160;
            DataContext = this;
            Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush");
            Owner = MainWindowVM.Instance.MainWindow;
        }

        #region 静态方法
        public static void Info(string msg, string title = "提示")
        {
            new MessageWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                Owner = MainWindowVM.Instance.MainWindow
            }.ShowDialog();
        }
        public static void Warn(string msg, string title = "警告")
        {
            new MessageWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("OrangeBrush"),
                Owner = MainWindowVM.Instance.MainWindow
            }.ShowDialog();
        }
        public static void Error(string msg, string title = "错误")
        {
            new MessageWin
            {
                Title = title,
                Message = msg,
                Foreground = (SolidColorBrush)Application.Current.FindResource("RedBrush"),
                Owner = MainWindowVM.Instance.MainWindow
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
                Owner = MainWindowVM.Instance.MainWindow
            }.ShowDialog();
        }
        public static string Input(string value = "", string msg = "", string title = "查找")
        {
            var n = new MessageWin
            {
                Title = title,
                Message = msg,
                IsConfirm = true,
                IsInput = true,
                InputValue = value,
                Foreground = (SolidColorBrush)Application.Current.FindResource("MainForeBrush"),
                Owner = MainWindowVM.Instance.MainWindow
            };
            if (n.ShowDialog() == true)
            {
                return n.InputValue;
            }
            return string.Empty;
        }
        #endregion

        #region 属性

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(MessageWin), new PropertyMetadata(null));

        public TextDocument InputDocument
        {
            get { return (TextDocument)GetValue(InputDocumentProperty); }
            set { SetValue(InputDocumentProperty, value); }
        }
        // Using a DependencyProperty as the backing store for InputDocument.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputDocumentProperty =
            DependencyProperty.Register("InputDocument", typeof(TextDocument), typeof(MessageWin), new PropertyMetadata(null));

        public bool IsConfirm
        {
            get { return (bool)GetValue(IsConfirmProperty); }
            set { SetValue(IsConfirmProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsConfirm.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsConfirmProperty =
            DependencyProperty.Register("IsConfirm", typeof(bool), typeof(MessageWin), new PropertyMetadata(false));


        public bool IsInput
        {
            get { return (bool)GetValue(IsInputProperty); }
            set { 
                SetValue(IsInputProperty, value);
                if (value) IsEditor = true;
            }
        }

        // Using a DependencyProperty as the backing store for IsInput.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsInputProperty =
            DependencyProperty.Register("IsInput", typeof(bool), typeof(MessageWin), new PropertyMetadata(false));


        public bool IsEditor
        {
            get { return (bool)GetValue(IsEditorProperty); }
            set { SetValue(IsEditorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsInput.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditorProperty =
            DependencyProperty.Register("IsEditor", typeof(bool), typeof(MessageWin), new PropertyMetadata(false));


        public string InputValue
        {
            get { return (string)GetValue(InputValueProperty); }
            set 
            { 
                SetValue(InputValueProperty, value);
                InputDocument = new TextDocument(value);
            }
        }

        // Using a DependencyProperty as the backing store for InputValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputValueProperty =
            DependencyProperty.Register("InputValue", typeof(string), typeof(MessageWin), new PropertyMetadata(null));



        public string OkContent
        {
            get { return (string)GetValue(OkContentProperty); }
            set { SetValue(OkContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OkContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OkContentProperty =
            DependencyProperty.Register("OkContent", typeof(string), typeof(MessageWin), new PropertyMetadata("确定"));



        public string CancelContent
        {
            get { return (string)GetValue(CancelContentProperty); }
            set { SetValue(CancelContentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CancelContent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CancelContentProperty =
            DependencyProperty.Register("CancelContent", typeof(string), typeof(MessageWin), new PropertyMetadata("取消"));



        public HorizontalAlignment MessageAlignment
        {
            get { return (HorizontalAlignment)GetValue(MessageAlignmentProperty); }
            set { SetValue(MessageAlignmentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MessageAlignment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageAlignmentProperty =
            DependencyProperty.Register("MessageAlignment", typeof(HorizontalAlignment), typeof(MessageWin), new PropertyMetadata(HorizontalAlignment.Center));
        #endregion

        #region 事件

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (IsEditor)
                InputValue = InputDocument.Text;
            this.DialogResult = true;
        }

        private void MessageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsInput)
            {
                /*
                //默认焦点并全选
                InputBox.Focus();
                InputBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    InputBox.SelectAll();
                }));*/
                codeEditor.Focus();
                codeEditor.Dispatcher.BeginInvoke(new Action(() =>
                {
                    codeEditor.SelectAll();
                }));
            }
            //else if (IsConfirm)
            //{
            //    CancelBtn.Focus();
            //}
            //else
            //{
            //    OKBtn.Focus();
            //}
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();//拖拽移动窗口
            e.Handled = true;
        }
        #endregion

        private void codeEditor_TextChanged(object sender, EventArgs e)
        {
            //自动高度 2:行高偏差 10:滚动条
            codeEditor.Height = codeEditor.LineCount * (codeEditor.FontSize + 2) + 10;
        }
    }
}
