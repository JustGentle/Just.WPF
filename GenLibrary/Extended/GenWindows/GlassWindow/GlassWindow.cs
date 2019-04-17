using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Standard;
using GenLibrary.MVVM.Base;
using System.ComponentModel;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Animation;
//using System.Windows.Interactivity;
//using WindowChrome = GenLibrary.ControlzEx.Microsoft.Windows.Shell;
using GenLibrary.ControlzEx.Behaviors;
using System.Media;
namespace GenLibrary.GenWindows
{
    /// <summary>
    /// 操作系统类型
    /// </summary>
    public enum OSType
    {
        Windows7,
        Windows8,
        Windows10
    }
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    [TemplatePart(Name = "PART_borderFrame", Type = typeof(Border))]
    public class GlassWindow : Window
    {
        static OSType _OSType = OSType.Windows8;
        //WindowChromeBehavior windowChromeBehavior; //Windows附加行为
        #region 使用系统毛玻璃,Win10下可用
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal void EnableBlur()
        {
            var WindowPtr = new WindowInteropHelper(this).Handle;

            var accent = new AccentPolicy()
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND,
                AccentFlags = 0x20 | 0x40 | 0x80 | 0x100,
                //GradientColor = 0x000000FF,
                //AnimationId = 
            };
            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData()
            {
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };
            SetWindowCompositionAttribute(WindowPtr, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
        #endregion
        #region 使用系统毛玻璃,Win7下可用
        [DllImport("DwmApi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);
        [DllImport("dwmapi.dll", PreserveSig = false)]
        static extern bool DwmIsCompositionEnabled();

        #endregion
        #region 窗体初始化
        static GlassWindow()
        {
            //启用自定义窗口风格
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GlassWindow), new FrameworkPropertyMetadata(typeof(GlassWindow)));
            //检测当前系统
            Version currentVersion = Environment.OSVersion.Version;
            Version compareToVersion = new Version("6.2");
            if (currentVersion.Major >= 6)
            {
                if (currentVersion.CompareTo(compareToVersion) > 0) //Win10系统
                {
                    _OSType = OSType.Windows10;
                }
                else if (currentVersion.MajorRevision < 2) //Win8之下的系统
                {
                    _OSType = OSType.Windows7;
                }
                else
                {
                    _OSType = OSType.Windows8;
                }
            }
            else
            {
                _OSType = OSType.Windows8;
            }
            //_OSType = OSType.Windows8;
        }
        
        public GlassWindow()
        {
                        
            //根据系统不同，设定是否容许透明
            if(_OSType == OSType.Windows7) //Win7必须不透明，才能使用系统毛玻璃
            {
                AllowsTransparency = false;
                //Standard.WindowResizer.Resizable(this);
                //this.InitializeWindowChromeBehavior(-1);
            }               
            else
            {
                //WindowStyle = System.Windows.WindowStyle.None;
                //AllowsTransparency = true;
                //this.InitializeWindowChromeBehavior(1);
                this.Background = Brushes.Transparent;
            }
            OSType = _OSType;//设置程序所属的操作系统，以便于选择不同的窗口模板
        }

        private void InitializeWindowChromeBehavior(double GlassFrameThickness)
        {
            //windowChromeBehavior = new WindowChromeBehavior();
            //windowChromeBehavior.CaptionHeight = 0;
            //windowChromeBehavior.ResizeBorderThickness = new Thickness(4);
            //windowChromeBehavior.CornerRadius = new CornerRadius(0);
            //windowChromeBehavior.GlassFrameThickness= new Thickness(GlassFrameThickness);
            //windowChromeBehavior.UseAeroCaptionButtons = false;
            
            //Interaction.GetBehaviors(this).Add(windowChromeBehavior);
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //WindowResizer.Resizable<GlassWindow>(this);//窗口可调整类
            Border borderTitle = this.Template.FindName("borderTitle", this) as Border;
            if (borderTitle != null)
            {
                //鼠标拖动
                borderTitle.MouseMove += delegate (object sender, MouseEventArgs e)

                {

                    if (e.LeftButton == MouseButtonState.Pressed)

                    {

                        WindowResizer.DragMoveEx<GlassWindow>(this);
                        //this.DragMove();

                    }

                };
                //鼠标双击事件
                borderTitle.MouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e)
                {
                    if (this.ResizeMode == ResizeMode.NoResize)
                    {
                        return;
                    }
                    if (e.ClickCount == 2)
                        MaxWindow();
                };               
            }

        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            #region 有阴影效果不使用模态对话框动画
            //if (_OSType == OSType.Windows7) return;
            //IntPtr hwnd = new WindowInteropHelper(this).Handle;
            //HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
            #endregion
            #region 启用系统毛玻璃效果      
            //Border borderFrame = this.Template.FindName("PART_borderFrame", this) as Border;
            
            if (_OSType == OSType.Windows10)
            {
                EnableBlur();
            }
            else if (_OSType == OSType.Windows7)
            {
                //IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
                //HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                //mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);

                //MARGINS margins = new MARGINS();
                //margins.cxLeftWidth = -1;
                //margins.cxRightWidth = -1;
                //margins.cyTopHeight = -1;
                //margins.cyBottomHeight = -1;

                //DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
            }
            #endregion
        }

        #endregion
        #region 模态窗口的闪动(用于无边框窗口,这里有阴影效果不使用)
        const int WM_SETCURSOR = 0x20;
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SETCURSOR)
            {
                // 0x202fffe: WM_LBUTTONUP and HitTest
                // 0x201fffe: WM_LBUTTONDOWN and HitTest
                if (lParam.ToInt32() == 0x202fffe || lParam.ToInt32() == 0x201fffe)
                {                    
                    if (OwnedWindows.Count > 0)
                    {
                        foreach (Window child in OwnedWindows)
                        {
                            if (child.IsActive)
                            {
                                SystemSounds.Asterisk.Play();
                                child.Blink();
                                handled = true;
                                break;
                            }
                        }
                    }
                }
            }
            return IntPtr.Zero;
        }
       
        #endregion
        #region 内容绑定
        /// <summary>
        /// Identifies the BackgroundContent dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBackContentProperty = DependencyProperty.Register("TitleBackContent", typeof(object), typeof(GlassWindow));
        /// <summary>
        /// 获取或者设置标题区域背部内容
        /// </summary>
        public object TitleBackContent
        {
            get { return GetValue(TitleBackContentProperty); }
            set { SetValue(TitleBackContentProperty, value); }
        }
        /// <summary>
        /// Defines the ContentSource dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleRightContentProperty = DependencyProperty.Register("TitleRightContent", typeof(object), typeof(GlassWindow));
        /// <summary>
        ///获取或者设置标题区域右侧内容
        /// </summary>
        public object TitleRightContent
        {
            get { return GetValue(TitleRightContentProperty); }
            set { SetValue(TitleRightContentProperty, value); }
        }

        /// <summary>
        /// Defines the ContentSource dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleLeftContentProperty = DependencyProperty.Register("TitleLeftContent", typeof(object), typeof(GlassWindow));
        /// <summary>
        ///获取或者设置标题区域左侧内容
        /// </summary>
        public object TitleLeftContent
        {
            get { return GetValue(TitleLeftContentProperty); }
            set { SetValue(TitleLeftContentProperty, value); }
        }
        #endregion
        #region 窗口操作命令
        //最小化操作
        RelayCommand _MinWindowCommand;
        public RelayCommand MinWindowCommand
        {
            get
            {
                if (_MinWindowCommand == null)
                {
                    _MinWindowCommand = new RelayCommand(MinWindow, CanMin);
                }
                return _MinWindowCommand;
            }
            set
            {
                _MinWindowCommand = value;
            }
        }
        bool CanMin() //是否能够执行
        {
            return this.ResizeMode != ResizeMode.NoResize;
        }
        void MinWindow() //执行命令
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }
        //最大化操作
        RelayCommand _MaxWindowCommand;
        public RelayCommand MaxWindowCommand
        {
            get
            {
                if (_MaxWindowCommand == null)
                {
                    _MaxWindowCommand = new RelayCommand(MaxWindow, CanMax);
                }
                return _MaxWindowCommand;
            }
            set
            {
                _MaxWindowCommand = value;
            }
        }
        bool CanMax() //是否能够执行
        {
            return this.ResizeMode != ResizeMode.NoResize;
        }
        void MaxWindow() //执行命令
        {
            if (this.WindowState != System.Windows.WindowState.Maximized)
                this.WindowState = System.Windows.WindowState.Maximized;
            else
                this.WindowState = System.Windows.WindowState.Normal;
        }
        //关闭操作
        RelayCommand _CloseWindowCommand;
        public RelayCommand CloseWindowCommand
        {
            get
            {
                if (_CloseWindowCommand == null)
                {
                    _CloseWindowCommand = new RelayCommand(CloseWindow, CanClose);
                }
                return _CloseWindowCommand;
            }
            set
            {
                _CloseWindowCommand = value;
            }
        }
        bool CanClose() //是否能够执行
        {
            return true;
        }
        void CloseWindow() //执行命令
        {
            this.Close();
        }
        #endregion
        #region 窗口按钮属性设定
        [Description("设定最小化按钮的可见性")]
        [Category("控制按钮")]
        public Visibility MinButtonVisibility
        {
            get { return (Visibility)GetValue(MinButtonVisibilityProperty); }
            set
            {
                SetValue(MinButtonVisibilityProperty, value);
            }
        }
        public static readonly DependencyProperty MinButtonVisibilityProperty =
          DependencyProperty.Register("MinButtonVisibility", typeof(Visibility), typeof(GlassWindow), new PropertyMetadata(Visibility.Visible));
        [Description("设定最大化按钮的可见性")]
        [Category("控制按钮")]
        public Visibility MaxButtonVisibility
        {
            get { return (Visibility)GetValue(MaxButtonVisibilityProperty); }
            set
            {
                SetValue(MaxButtonVisibilityProperty, value);
            }
        }
        public static readonly DependencyProperty MaxButtonVisibilityProperty =
          DependencyProperty.Register("MaxButtonVisibility", typeof(Visibility), typeof(GlassWindow), new PropertyMetadata(Visibility.Visible));
        [Description("设定关闭按钮的可见性")]
        [Category("控制按钮")]
        public Visibility CloseButtonVisibility
        {
            get { return (Visibility)GetValue(CloseButtonVisibilityProperty); }
            set
            {
                SetValue(CloseButtonVisibilityProperty, value);
            }
        }
        public static readonly DependencyProperty CloseButtonVisibilityProperty =
          DependencyProperty.Register("CloseButtonVisibility", typeof(Visibility), typeof(GlassWindow), new PropertyMetadata(Visibility.Visible));       

        #endregion
        #region 阴影相关，目前不用
        [Description("窗口边框阴影容纳尺寸")]
        [Category("窗口属性")]
        public Thickness WindowShadowMargin
        {
            get { return (Thickness)GetValue(WindowShadowMarginProperty); }
            set { SetValue(WindowShadowMarginProperty, value); }
        }
        public static readonly DependencyProperty WindowShadowMarginProperty =
          DependencyProperty.Register("WindowShadowMargin", typeof(Thickness), typeof(GlassWindow), new FrameworkPropertyMetadata(new Thickness(10)));
        #endregion
        #region 窗口属性
        [Description("设置内边框厚度")]
        public Thickness InnerBorderThinkness
        {
            get { return (Thickness)GetValue(InnerBorderThinknessProperty); }
            set { SetValue(InnerBorderThinknessProperty, value); }
        }
        public static readonly DependencyProperty InnerBorderThinknessProperty =
            DependencyProperty.Register("InnerBorderThinkness", typeof(Thickness), typeof(GlassWindow), new PropertyMetadata(new Thickness(1)));

        [Description("设置窗口透明区域颜色")]
        public Brush GlassBackgroundBrush
        {
            get { return (Brush)GetValue(GlassBackgroundBrushProperty); }
            set { SetValue(GlassBackgroundBrushProperty, value); }
        }

        public static readonly DependencyProperty GlassBackgroundBrushProperty =
            DependencyProperty.Register("GlassBackgroundBrush", typeof(Brush), typeof(GlassWindow), new PropertyMetadata(new SolidColorBrush(Colors.Black)));
        [Description("设置当前程序运行的操作系统类型")]
        public OSType OSType
        {
            get { return (OSType)GetValue(OSTypeProperty); }
            set { SetValue(OSTypeProperty, value); }
        }
        public static readonly DependencyProperty OSTypeProperty =
           DependencyProperty.Register("OSType", typeof(OSType), typeof(GlassWindow), new PropertyMetadata(OSType.Windows8));
        #endregion
        #region 窗口关闭状态动画(不使用)
        //bool isclose = false;
        //Storyboard ClosingAnimation;
        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    base.OnClosing(e);
        //    if (!isclose)
        //    {

        //        CreateClosingWindowAnimation();
        //        e.Cancel = true;
        //    }
        //}
        //void CreateClosingWindowAnimation()
        //{
        //    ClosingAnimation = new Storyboard();
        //    Storyboard.SetTarget(ClosingAnimation, this);//设置动画的应用对象

        //    ClosingAnimation.Completed += ClosingAnimation_Completed;
        //    TransformGroup group = new TransformGroup();
        //    RotateTransform rtf = new RotateTransform(0);
        //    group.Children.Add(rtf);
        //    ScaleTransform rtf2 = new ScaleTransform(1, 1);
        //    group.Children.Add(rtf2);
        //    this.RenderTransform = group; this.RenderTransformOrigin = new Point(0.5, 0.5);

        //    Storyboard.SetTargetProperty(ClosingAnimation, new PropertyPath("Opacity"));
        //    DoubleAnimation OpacityAnimation = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(350)));
        //    ClosingAnimation.Children.Add(OpacityAnimation);
        //    ClosingAnimation.Begin();


        //    Storyboard.SetTargetProperty(ClosingAnimation, new PropertyPath("RenderTransform.Children[0].Angle"));
        //    DoubleAnimation dbAscending = new DoubleAnimation(0, 180, new Duration(TimeSpan.FromMilliseconds(350)));
        //    ClosingAnimation.Children.Add(dbAscending);
        //    ClosingAnimation.Begin();

        //    Storyboard.SetTargetProperty(ClosingAnimation, new PropertyPath("RenderTransform.Children[1].ScaleX"));
        //    DoubleAnimation dbScaleX = new DoubleAnimation(1, 0.3, new Duration(TimeSpan.FromMilliseconds(350)));
        //    ClosingAnimation.Children.Add(dbScaleX);
        //    ClosingAnimation.Begin();

        //    Storyboard.SetTargetProperty(ClosingAnimation, new PropertyPath("RenderTransform.Children[1].ScaleY"));
        //    DoubleAnimation dbScaleY = new DoubleAnimation(1, 0.1, new Duration(TimeSpan.FromMilliseconds(350)));
        //    ClosingAnimation.Children.Add(dbScaleY);
        //    ClosingAnimation.Begin();
        //}
        //private void ClosingAnimation_Completed(object sender, EventArgs e)
        //{
        //    isclose = true;
        //    this.Close();
        //}
        #endregion
        #region 窗口启动动画(不使用)    
        //protected override void OnInitialized(EventArgs e)
        //{
        //    base.OnInitialized(e);
        //    CreateLoadWindowAnimation();
        //}
        //Storyboard LoadAnimation;
        //void CreateLoadWindowAnimation()
        //{
        //    LoadAnimation = new Storyboard();
        //    Storyboard.SetTarget(LoadAnimation, this);//设置动画的应用对象

        //    TransformGroup group = new TransformGroup();
        //    ScaleTransform rtf2 = new ScaleTransform();
        //    group.Children.Add(rtf2);
        //    this.RenderTransform = group; this.RenderTransformOrigin = new Point(0, 1);

        //    Storyboard.SetTargetProperty(LoadAnimation, new PropertyPath("Opacity"));
        //    DoubleAnimation OpacityAnimation = new DoubleAnimation(0.4, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        //    LoadAnimation.Children.Add(OpacityAnimation);
        //    LoadAnimation.Begin();

        //    //Storyboard.SetTargetProperty(LoadAnimation, new PropertyPath("RenderTransform.Children[0].ScaleX"));
        //    //DoubleAnimation dbScaleX = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        //    //LoadAnimation.Children.Add(dbScaleX);
        //    //LoadAnimation.Begin();

        //    //Storyboard.SetTargetProperty(LoadAnimation, new PropertyPath("RenderTransform.Children[0].ScaleY"));
        //    //DoubleAnimation dbScaleY = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        //    //LoadAnimation.Children.Add(dbScaleY);
        //    //LoadAnimation.Begin();
        //}
        #endregion
        #region 窗口忙提示
        public static readonly DependencyProperty IsBusyProperty =
         DependencyProperty.Register("IsBusy", typeof(bool), typeof(GlassWindow), new PropertyMetadata(false));
        /// <summary>
        /// 表明当前窗口是否正忙
        /// </summary>
        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set
            {
                SetValue(IsBusyProperty, value);
            }
        }
        /// <summary>
        /// 工作完成时，复位掉状态信息
        /// </summary>
        public virtual void WorkCmpl()
        {
            IsBusy = false;
        }
        public static readonly DependencyProperty ProgressBarValueProperty = DependencyProperty.Register(
      "ProgressBarValue",typeof(double),typeof(GlassWindow),new PropertyMetadata(0.0));
        /// <summary>
        /// 进度条值
        /// </summary>
        public double ProgressBarValue
        {
            get
            {
                return (double)GetValue(ProgressBarValueProperty);
            }
            set
            {
                SetValue(ProgressBarValueProperty, value);
            }
        }

        public static readonly DependencyProperty BusyContentTemplateProperty = DependencyProperty.Register(
     "BusyContentTemplate", typeof(DataTemplate), typeof(GlassWindow), new PropertyMetadata(null));
        /// <summary>
        /// 进度条数据模板
        /// </summary>
        public DataTemplate BusyContentTemplate
        {
            get
            {
                return (DataTemplate)GetValue(BusyContentTemplateProperty);
            }
            set
            {
                SetValue(BusyContentTemplateProperty, value);
            }
        }

        public static readonly DependencyProperty BusyIndicatorStyleProperty = DependencyProperty.Register(
     "BusyIndicatorStyle", typeof(Style), typeof(GlassWindow), new PropertyMetadata(null));
        /// <summary>
        /// 进度条风格设置
        /// </summary>
        public Style BusyIndicatorStyle
        {
            get
            {
                return (Style)GetValue(BusyIndicatorStyleProperty);
            }
            set
            {
                SetValue(BusyIndicatorStyleProperty, value);
            }
        }
        #endregion
    }//结束类
}//
