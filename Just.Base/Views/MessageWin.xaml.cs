using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Linq;
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
            set { SetValue(IsInputProperty, value); }
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

        public IHighlightingDefinition Syntax
        {
            get { return (IHighlightingDefinition)GetValue(SyntaxProperty); }
            set { SetValue(SyntaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Syntax.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SyntaxProperty =
            DependencyProperty.Register("Syntax", typeof(IHighlightingDefinition), typeof(MessageWin), new PropertyMetadata(HighlightingManager.Instance?.HighlightingDefinitions.FirstOrDefault(hl => hl.Name == "Json-Dark")));

        public Brush EditorForeground
        {
            get { return (Brush)GetValue(EditorForegroundProperty); }
            set { SetValue(EditorForegroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditorForeground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditorForegroundProperty =
            DependencyProperty.Register("EditorForeground", typeof(Brush), typeof(MessageWin), new PropertyMetadata(Brushes.Black));

        public Brush EditorBackground
        {
            get { return (Brush)GetValue(EditorBackgroundProperty); }
            set { SetValue(EditorBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditorBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditorBackgroundProperty =
            DependencyProperty.Register("EditorBackground", typeof(Brush), typeof(MessageWin), new PropertyMetadata(Brushes.White));

        public double EditorMinHeight
        {
            get { return (double)GetValue(EditorMinHeightProperty); }
            set { SetValue(EditorMinHeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditorMinHeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditorMinHeightProperty =
            DependencyProperty.Register("EditorMinHeight", typeof(double), typeof(MessageWin), new PropertyMetadata(10d));

        public bool EditorShowLineNumbers
        {
            get { return (bool)GetValue(EditorShowLineNumbersProperty); }
            set { SetValue(EditorShowLineNumbersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditorShowLineNumbers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditorShowLineNumbersProperty =
            DependencyProperty.Register("EditorShowLineNumbers", typeof(bool), typeof(MessageWin), new PropertyMetadata(false));

        public bool EditorWrap
        {
            get { return (bool)GetValue(EditorWrapProperty); }
            set 
            { 
                SetValue(EditorWrapProperty, value);
                if (value)
                    WrapMenuItem_Checked(null, null);
                else
                    WrapMenuItem_Unchecked(null, null);
            }
        }

        // Using a DependencyProperty as the backing store for EditorWrap.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditorWrapProperty =
            DependencyProperty.Register("EditorWrap", typeof(bool), typeof(MessageWin), new PropertyMetadata(false));


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
                if (IsEditor)
                {
                    codeEditor.Focus();
                }
                else
                {
                    //默认焦点并全选
                    InputBox.Focus();
                    InputBox.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        InputBox.SelectAll();
                    }));
                }
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
            if (e.ClickCount >= 2)
                SwitchWindowState();//双击切换窗口最大化
            else
                this.DragMove();//拖拽移动窗口
            e.Handled = true;
        }
        private void SwitchWindowState()
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else if(this.ResizeMode != ResizeMode.NoResize)
                this.WindowState = WindowState.Maximized;
        }
        /// <summary>
        /// 状态处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MessageWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowStyle != WindowStyle.None) return;

            //无边框时最大化会全屏覆盖任务栏,需要做处理
            //NoResize可以取消移动窗口到边缘自动最大化，但会导致最大化时边框显示异常
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.WindowStyle = WindowStyle.None;
                //隐藏边框,方便右上角直接激活关闭按钮
                this.LayoutRoot.BorderThickness = new Thickness(0);
            }
            else
            {
                this.LayoutRoot.BorderThickness = new Thickness(1);
            }
        }
        private void MessageWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalcEditorHeight();
        }
        #endregion

        #region 编辑器
        private void codeEditor_TextChanged(object sender, EventArgs e)
        {
            if (codeEditor.WordWrap)
                return;
            CalcEditorHeight();
        }
        private double CalcEditorHeight()
        {
            if (codeEditor.WordWrap)
            {
                codeEditor.Height = this.Height - 128;
            }
            else
            {
                //自动高度 2:行高偏差 10:滚动条
                codeEditor.Height = codeEditor.LineCount * (codeEditor.FontSize + 2) + 10;
            }
            return codeEditor.Height;
        } 

        private string _findText = string.Empty;
        private void CommandFind_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var text = MessageWin.Input(_findText);
            if (string.IsNullOrEmpty(text)) return;
            _findText = text;
            FindNextText(-1, _findText, false);
        }
        private int FindNextText(int start, string findText, bool previous)
        {
            var i = -1;
            if (string.IsNullOrEmpty(findText) || string.IsNullOrEmpty(codeEditor.Text)) return i;
            if (previous)
            {
                if (start == -1) start = codeEditor.SelectionStart;
                i = codeEditor.Text.LastIndexOf(findText, start);
            }
            else
            {
                if (start == -1) start = codeEditor.SelectionStart + codeEditor.SelectionLength;
                i = codeEditor.Text.IndexOf(findText, start);
            }
            if (i < 0) return i;
            codeEditor.Select(i, findText.Length);
            codeEditor.ScrollTo(codeEditor.Document.GetLineByOffset(i).LineNumber, codeEditor.CaretOffset);
            return i;
        }

        private void WrapMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            codeEditor.WordWrap = true;
            this.SizeToContent = SizeToContent.Manual;
            this.ResizeMode = ResizeMode.CanResize;
            CalcEditorHeight();
        }

        private void WrapMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            codeEditor.WordWrap = false;
            this.SizeToContent = SizeToContent.Height;
            this.ResizeMode = ResizeMode.NoResize;
            CalcEditorHeight();
        }
        #endregion

    }
}
