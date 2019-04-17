/********************************************************
 * File : WindowResizer
 * Version : 1.0.0.0
 * Author : Vito / ( Ye.Wang )
 * E-mail : vito2015@live.com / ( ye.wang@combuilder.com.sg )
 * Date：2014-05-04 13:04:56
 * Description :  
 *      窗口尺寸修改部件.
 * ChangedHistory :
 *      1) 加入对窗口的四周边框和顶点的拖拽改变大小支持.   - 2014-05-04 13:07:57
 *      2) 加入对窗口的最大化位置的自定义,并且让窗口在最大化后还原时类似系统窗口的拖拽标题栏也可以回复窗口 的最大化,而不是只能双击或者点击还原按钮来实现.  - - 2014-05-04 13:07:57
 *      
 ********************************************************/
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Linq;
using System.Reflection;
using Helper=GenLibrary.ControlzEx.Helper;
using System.Media;
namespace Standard
{
    /// <summary>
    /// 窗口消息探测器.
    /// </summary>
    /// <remarks>
    /// designed by : ye.wang
    /// lastest update : 2014-05-04 13:09:20
    /// version : 1.0
    /// </remarks>
    class WindowResizerImp
    {
        private const int WND_BORDER_DROPSHADOW_SIZE = 4;
        private readonly int agWidth = 12; //拐角宽度
        private readonly int bThickness = 4; // 边框宽度，鼠标捕捉感知，使用阴影时设定为10，不使用阴影时设定为4
        private Point mousePoint = new Point(); //鼠标坐标

        private int? _maxiX = null; // 窗口最大化时的左上角坐标X
        private int? _maxiY = null; // 窗口最大化时的左上角坐标Y
        private Func<int?> _maxiX_call = null;
        private Func<int?> _maxiY_call = null;
        private readonly bool _bUseCall = false; // 使用 xxx_call 还是 field,默认field

        private bool _bFailed = false;

        private Window _wndTarget = null;


        WindowResizerImp(Window wndTarget)
        {
            if (null == wndTarget)
                _bFailed = true;

            _wndTarget = wndTarget;

            _wndTarget.SourceInitialized += _wndTarget_SourceInitialized;
        }

        //public WindowResizerImp( Window wndTarget, int? maxiX = null, int? maxiY = null )
        //	: this( wndTarget )
        //{
        //	this._maxiX = maxiX;
        //	this._maxiY = maxiY;

        //	_bUseCall = false;
        //}

        public WindowResizerImp(Window wndTarget, Func<int?> maxiX_call = null, Func<int?> maxiY_call = null)
            : this(wndTarget)
        {
            this._maxiX_call = maxiX_call ?? (() => null);
            this._maxiY_call = maxiY_call ?? (() => null);

            _bUseCall = true;
        }


        void _wndTarget_SourceInitialized(object sender, EventArgs e)
        {
            addHook();
        }

        private void addHook()
        {
            if (_bFailed)
                return;

            HwndSource hwndSource = PresentationSource.FromVisual(_wndTarget) as HwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(new HwndSourceHook(WndProc));
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch ((WM)msg)
            {
                case WM.NCHITTEST:
                    {
                        return WmNCHitTest(lParam, ref handled);
                    }
                case WM.SETCURSOR:
                    {
                        WmSetCursor(lParam, ref handled);

                    }
                    break;


                case WM.GETMINMAXINFO:
                    {
                        WmGetMinMaxInfo(hwnd, lParam);
                        handled = true;

                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void WmSetCursor(IntPtr lParam, ref bool handled)
        {
            // 0x202fffe: WM_LBUTTONUP and HitTest
            // 0x201fffe: WM_LBUTTONDOWN and HitTest
            if (lParam.ToInt32() == 0x202fffe || lParam.ToInt32() == 0x201fffe)
            {
                if (!_wndTarget.IsActive)
                {
                    if (_wndTarget.OwnedWindows.Count > 0)
                    {
                        foreach (Window child in _wndTarget.OwnedWindows)
                        {
                            if (child.IsActive)
                            {
                                // FlashWindowEx cann't use for non-border window...
                                child.Blink();
                                handled = true;
                                return;
                            }
                        }
                    }
                    else
                    {
                        // if target window  has 0 children 
                        // then , find current active wnd and blink it.
                        // eg: MessageBox.Show("hello!"); the box without
                        // owner, when setcursor to target window , we will
                        // blink this box.
                        IntPtr pWnd = NativeMethods.GetActiveWindow();
                        if (pWnd != IntPtr.Zero)
                        {
                            HwndSource hs = HwndSource.FromHwnd(pWnd);

                            Window activeWnd = null == hs ? null : hs.RootVisual as Window;
                            if (null != activeWnd && activeWnd.IsActive)
                            {
                                activeWnd.Blink();
                                handled = true;
                                return;
                            }
                        }
                        else
                        {
                            var wnds = Application.Current.Windows;
                            if (null != wnds && wnds.Count > 1)
                            {

                                Window modalWnd = wnds.OfType<Window>().Where(p => p != _wndTarget).FirstOrDefault(p => p.IsModal());
                                if (null != modalWnd)
                                {
                                    modalWnd.Activate();
                                    modalWnd.Blink();
                                    handled = true;
                                    return;
                                }

                            }
                        }
                    }
                }
                else
                {// 父窗口在上面 而模态的在下面的情况

                    var wnds = Application.Current.Windows;
                    if (null != wnds && wnds.Count > 1)
                    {

                        Window modalWnd = wnds.OfType<Window>().Where(p => p != _wndTarget).FirstOrDefault(p => p.IsModal());
                        if (null != modalWnd)
                        {
                            modalWnd.Activate();
                            modalWnd.Blink();
                            handled = true;
                            return;
                        }

                    }
                }
            }

            handled = false;
        }


        private IntPtr WmNCHitTest(IntPtr lParam, ref bool handled)
        {
            // Update cursor point
            // The low-order word specifies the x-coordinate of the cursor.
            // #define GET_X_LPARAM(lp) ((int)(short)LOWORD(lp))
            this.mousePoint.X = (int)(short)(lParam.ToInt32() & 0xFFFF);
            // The high-order word specifies the y-coordinate of the cursor.
            // #define GET_Y_LPARAM(lp) ((int)(short)HIWORD(lp))
            this.mousePoint.Y = (int)(short)(lParam.ToInt32() >> 16);

            double Top = Helper.DpiHelper.TransformToDeviceY(_wndTarget, _wndTarget.Top); 
            double Left = Helper.DpiHelper.TransformToDeviceX(_wndTarget, _wndTarget.Left);
            double ActualWidth = Helper.DpiHelper.TransformToDeviceX(_wndTarget, _wndTarget.ActualWidth);
            double ActualHeight = Helper.DpiHelper.TransformToDeviceY(_wndTarget, _wndTarget.ActualHeight);

            // Do hit test
            handled = true;
            if (Math.Abs(this.mousePoint.Y - Top) <= Helper.DpiHelper.TransformToDeviceY(this.agWidth)
                && Math.Abs(this.mousePoint.X - Left) <= Helper.DpiHelper.TransformToDeviceX(this.agWidth))
            { // Top-Left
                return new IntPtr((int)NativeMethods.HitTest.HTTOPLEFT);
            }
            else if (Math.Abs(ActualHeight + Top - this.mousePoint.Y) <= Helper.DpiHelper.TransformToDeviceY(this.agWidth)
                && Math.Abs(this.mousePoint.X - Left) <= Helper.DpiHelper.TransformToDeviceX(this.agWidth))
            { // Bottom-Left
                return new IntPtr((int)NativeMethods.HitTest.HTBOTTOMLEFT);
            }
            else if (Math.Abs(this.mousePoint.Y - Top) <= Helper.DpiHelper.TransformToDeviceY(this.agWidth/2)
                && Math.Abs(ActualWidth + Left - this.mousePoint.X) <= Helper.DpiHelper.TransformToDeviceX(this.agWidth/2))
            { // Top-Right顶部减少感应区域，避免影响关闭按钮
                return new IntPtr((int)NativeMethods.HitTest.HTTOPRIGHT);
            }
            else if (Math.Abs(ActualWidth + Left - this.mousePoint.X) <= Helper.DpiHelper.TransformToDeviceX(this.agWidth)
                && Math.Abs(ActualHeight + Top - this.mousePoint.Y) <= Helper.DpiHelper.TransformToDeviceY(this.agWidth))
            { // Bottom-Right 
                return new IntPtr((int)NativeMethods.HitTest.HTBOTTOMRIGHT);
            }
            else if (Math.Abs(this.mousePoint.X - Left) <= Helper.DpiHelper.TransformToDeviceX(this.bThickness))
            { // Left
                return new IntPtr((int)NativeMethods.HitTest.HTLEFT);
            }
            else if (Math.Abs(ActualWidth + Left - this.mousePoint.X) <= Helper.DpiHelper.TransformToDeviceX(this.bThickness))
            { // Right
                return new IntPtr((int)NativeMethods.HitTest.HTRIGHT);
            }
            else if (Math.Abs(this.mousePoint.Y - Top) <= Helper.DpiHelper.TransformToDeviceY(this.bThickness))
            { // Top
                return new IntPtr((int)NativeMethods.HitTest.HTTOP);
            }
            else if (Math.Abs(ActualHeight + Top - this.mousePoint.Y) <= Helper.DpiHelper.TransformToDeviceY(this.bThickness))
            { // Bottom
                return new IntPtr((int)NativeMethods.HitTest.HTBOTTOM);
            }
            else
            {
                handled = false;
                return IntPtr.Zero;
            }
        }

        private void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam)
        {
            #region // old code  commented out by ye.wang 2014-05-04 10:33:18

            //// MINMAXINFO structure
            //NativeMethods.MINMAXINFO minMaxInfo = (NativeMethods.MINMAXINFO)Marshal.PtrToStructure( lParam, typeof( NativeMethods.MINMAXINFO ) );

            //// Get the handle of the nearest monitor to the window
            ////WindowInteropHelper interopHelper = new WindowInteropHelper( this );
            //IntPtr hMonitor = NativeMethods.MonitorFromWindow( _wndTarget.GetHandle(), NativeMethods.MONITOR_DEFAULTTONEAREST );

            //// Get monitor information
            //NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();
            //monitorInfo.cbSize = Marshal.SizeOf( monitorInfo );
            //NativeMethods.GetMonitorInfo( new HandleRef( this, hMonitor ), monitorInfo );

            //// Get HwndSource
            //HwndSource source = HwndSource.FromHwnd( _wndTarget.GetHandle() );
            //if ( source == null )
            //	// Should never be null
            //	throw new Exception( "Cannot get HwndSource instance." );
            //if ( source.CompositionTarget == null )
            //	// Should never be null
            //	throw new Exception( "Cannot get HwndTarget instance." );

            //// Get working area rectangle
            //NativeMethods.RECT workingArea = monitorInfo.rcWork;

            //Console.WriteLine( string.Format( "left:{0},top:{1},right:{2},bottom:{3}", monitorInfo.rcWork.Left,
            //	monitorInfo.rcWork.Top, monitorInfo.rcWork.Right, monitorInfo.rcWork.Bottom ) );

            //// Get transformation matrix
            //System.Windows.Media.Matrix matrix = source.CompositionTarget.TransformFromDevice;


            //// Convert working area rectangle to DPI-independent values
            //Point convertedSize =
            //	matrix.Transform( new Point(
            //			workingArea.Right - workingArea.Left,
            //			workingArea.Bottom - workingArea.Top
            //			) );
            //Point convertedPosion =
            //	matrix.Transform( new Point(
            //			workingArea.Left, workingArea.Top
            //			) );
            //// Set the maximized size of the window
            //minMaxInfo.ptMaxSize.X = (int)convertedSize.X+6;
            //minMaxInfo.ptMaxSize.Y = (int)convertedSize.Y+6;

            //// Set the position of the maximized window
            //minMaxInfo.ptMaxPosition.X = (int)convertedPosion.X-WND_BORDER_DROPSHADOW_SIZE;
            //minMaxInfo.ptMaxPosition.Y = (int)convertedPosion.Y-WND_BORDER_DROPSHADOW_SIZE;

            //Console.WriteLine( string.Format( "MaxInfo : left:{0},top:{1},right:{2},bottom:{3}",
            //	convertedPosion.X, convertedPosion.Y, convertedSize.X, convertedSize.Y ) );

            //Marshal.StructureToPtr( minMaxInfo, lParam, true );

            #endregion // // old code

            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));


            IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, (uint)MonitorOptions.MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                NativeMethods.MONITORINFOEX monitorInfo = new NativeMethods.MONITORINFOEX();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                NativeMethods.GetMonitorInfo(new HandleRef(this, monitor), monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;

                //mmi.ptMaxPosition.X = ( null != this.MaxiX ? this.MaxiX.Value : Math.Abs( rcWorkArea.Left - rcMonitorArea.Left ) ) - WND_BORDER_DROPSHADOW_SIZE;
                //mmi.ptMaxPosition.Y = ( null != this.MaxiY ? this.MaxiY.Value : Math.Abs( rcWorkArea.Top - rcMonitorArea.Top ) ) - WND_BORDER_DROPSHADOW_SIZE;


                if (!_bUseCall)
                {// use field 
                    mmi.ptMaxPosition.x = (null != this._maxiX ? this._maxiX.Value : Math.Abs(rcWorkArea.Left - rcMonitorArea.Left)) - WND_BORDER_DROPSHADOW_SIZE;
                    mmi.ptMaxPosition.y = (null != this._maxiY ? this._maxiY.Value : Math.Abs(rcWorkArea.Top - rcMonitorArea.Top)) - WND_BORDER_DROPSHADOW_SIZE;
                }
                else
                {
                    if (null == this._maxiX_call)
                        this._maxiX_call = () => null;
                    if (null == this._maxiY_call)
                        this._maxiY_call = () => null;

                    int? ret_x = this._maxiX_call.Invoke();
                    int? ret_y = this._maxiY_call.Invoke();

                    mmi.ptMaxPosition.x = (null != ret_x ? ret_x.Value : Math.Abs(rcWorkArea.Left - rcMonitorArea.Left)) - WND_BORDER_DROPSHADOW_SIZE;
                    mmi.ptMaxPosition.y = (null != ret_y ? ret_y.Value : Math.Abs(rcWorkArea.Top - rcMonitorArea.Top)) - WND_BORDER_DROPSHADOW_SIZE;
                }

                mmi.ptMaxSize.x=
                    Math.Abs(
                    Math.Abs(rcWorkArea.Right - rcWorkArea.Left) + WND_BORDER_DROPSHADOW_SIZE - mmi.ptMaxPosition.x);
                mmi.ptMaxSize.y = Math.Abs(
                    Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top) + WND_BORDER_DROPSHADOW_SIZE - mmi.ptMaxPosition.y);
                mmi.ptMinTrackSize.x = (int)this._wndTarget.MinWidth;
                mmi.ptMinTrackSize.y = (int)this._wndTarget.MinHeight;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }


    }


    class DialogBlinker<TWindow> where TWindow : Window
    {
        static Storyboard blinkStoryboard;
        static DoubleAnimation doubleAnimation;
        TWindow targetWindow = null;
        static DialogBlinker()
        {
            //blinkStoryboard = InitBlinkStory();
            //dropShadowEffect = InitDropShadowEffect();
            blinkStoryboard = new Storyboard();
             doubleAnimation = new DoubleAnimation(0.65, 1, new Duration(TimeSpan.FromMilliseconds(350)));
            doubleAnimation.RepeatBehavior = new RepeatBehavior(3);
            blinkStoryboard.Children.Add(doubleAnimation);
        }


        internal DialogBlinker(TWindow target)
        {
            targetWindow = target;

          
            Storyboard.SetTarget(doubleAnimation, targetWindow);
            Storyboard.SetTargetProperty(doubleAnimation, new PropertyPath("(Window.Opacity)"));
            

            blinkStoryboard.Completed += blinkStoryboard_Completed;


        }
        void blinkStoryboard_Completed(object sender, EventArgs e)
        {
            blinkStoryboard.Stop();
            blinkStoryboard.Completed -= blinkStoryboard_Completed;
        }

        public void Blink()
        {
            if (null != targetWindow)
            {

                //if (null == NameScope.GetNameScope(targetWindow))
                //    NameScope.SetNameScope(targetWindow, new NameScope());

                //originalEffect = targetWindow.Effect;

                //if (null == targetWindow.Effect || targetWindow.Effect.GetType() != typeof(DropShadowEffect))
                //    targetWindow.Effect = dropShadowEffect;


                //targetWindow.RegisterName("_blink_effect", targetWindow.Effect);

                SystemSounds.Asterisk.Play();
                //Storyboard.SetTargetName(blinkStoryboard.Children[0], "_blink_effect");
                targetWindow.FlashWindowEx();
                //blinkStoryboard.Begin(targetWindow, true);
                blinkStoryboard.Begin();
                //targetWindow.UnregisterName("_blink_effect");


            }
        }

        private static Storyboard InitBlinkStory()
        {
            #region xaml code

            /*
		<Storyboard x:Key="BlinkStory">
			<DoubleAnimationUsingKeyFrames Storyboard.TargetProperty="(UIElement.Effect).(DropShadowEffect.BlurRadius)" Storyboard.TargetName="border">
				<EasingDoubleKeyFrame KeyTime="0" Value="8">
					<EasingDoubleKeyFrame.EasingFunction>
						<ElasticEase EasingMode="EaseOut"/>
					</EasingDoubleKeyFrame.EasingFunction>
				</EasingDoubleKeyFrame>
				<EasingDoubleKeyFrame KeyTime="0:0:0.3" Value="26">
					<EasingDoubleKeyFrame.EasingFunction>
						<ElasticEase EasingMode="EaseOut"/>
					</EasingDoubleKeyFrame.EasingFunction>
				</EasingDoubleKeyFrame>
			</DoubleAnimationUsingKeyFrames>
		</Storyboard>
			 */

            #endregion // xaml code

            Storyboard storyboard = new Storyboard();

            //DoubleAnimationUsingKeyFrames keyFrames = new DoubleAnimationUsingKeyFrames();

            //EasingDoubleKeyFrame kt1 = new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0)));
            //EasingDoubleKeyFrame kt2 = new EasingDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)));

            //kt1.EasingFunction = new ElasticEase() { EasingMode = EasingMode.EaseOut };
            //kt2.EasingFunction = new ElasticEase() { EasingMode = EasingMode.EaseOut };

            //keyFrames.KeyFrames.Add(kt1);
            //keyFrames.KeyFrames.Add(kt2);

            //storyboard.Children.Add(keyFrames);
            //Storyboard.SetTargetProperty(keyFrames, new PropertyPath(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty));
           
            return storyboard;
        }

        private static DropShadowEffect InitDropShadowEffect()
        {
            DropShadowEffect dropShadowEffect = new DropShadowEffect();
            dropShadowEffect.BlurRadius = 8;
            dropShadowEffect.ShadowDepth = 0;
            dropShadowEffect.Direction = 0;
            dropShadowEffect.Color = System.Windows.Media.Colors.Black;
            return dropShadowEffect;
        }
    }

    /// <summary>
    /// 窗口修改器扩展类.
    /// </summary>
    /// <remarks>
    /// designed by : ye.wang
    /// lastest update : 2014-05-04 13:09:20
    /// version : 1.0
    /// </remarks>
    public static class WindowResizer
    {
        public static bool IsModal<TWindow>(this TWindow wnd) where TWindow : Window
        {
            return (bool)typeof(TWindow).GetField("_showingAsDialog", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(wnd);
        }


        static WindowResizer()
        {
        }

        /// <summary>
        /// 为窗口提供拖拽四周边框和四角进行改变窗口大小, 完善最大化支持(最大化时支持按住鼠标左键拖拽取消最大化)
        /// </summary>
        /// <typeparam name="TWindow"></typeparam>
        /// <param name="wnd"></param>
        public static void Resizable<TWindow>(this TWindow wnd) where TWindow : Window
        {
            if (null == wnd)
                return;

            new WindowResizerImp(wnd);
        }

        /// <summary>
        ///  为窗口提供拖拽四周边框和四角进行改变窗口大小, 完善最大化支持(最大化时支持按住鼠标左键拖拽取消最大化),并提供在最大化时自定义窗口的顶点位置.
        /// </summary>
        /// <typeparam name="TWindow"></typeparam>
        /// <param name="wnd"></param>
        /// <param name="maxiXCallback"></param>
        /// <param name="maxiYCallback"></param>
        public static void Resizable<TWindow>(this TWindow wnd, Func<int?> maxiXCallback, Func<int?> maxiYCallback) where TWindow : Window
        {
            if (null != wnd)
            {
                Func<int?> x_call = () => null;
                Func<int?> y_call = () => null;
                x_call = maxiXCallback ?? x_call;
                y_call = maxiYCallback ?? y_call;

                new WindowResizerImp(wnd, x_call, y_call);
            }
        }

        /// <summary>
        ///  允许使用在自定义窗口工作区的暴露区域上方按下其鼠标左键的鼠标来拖动窗口。
        ///  模拟鼠标命中非客户区-标题栏。
        /// </summary>
        /// <typeparam name="TWindow"></typeparam>
        /// <param name="wnd"></param>
        public static void DragMoveEx<TWindow>(this TWindow wnd) where TWindow : Window
        {
            if (null != wnd)
            {
                NativeMethods.SendMessage(wnd.GetHandle(), WM.NCLBUTTONDOWN, (IntPtr)NativeMethods.HitTest.HTCAPTION, IntPtr.Zero);
            }
        }

        public static void Blink<TWindow>(this TWindow wnd) where TWindow : Window
        {
            new DialogBlinker<TWindow>(wnd).Blink();
        }

    }
}//
