using Just.Base.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Just.WPF
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += App_Startup;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            //UI线程未捕获异常处理事件
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            //非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var msg = "非UI线程发生";
                msg += e.IsTerminating ? "致命错误" : "异常";
                if(e.ExceptionObject is Exception ex)
                {
                    Logger.Fatal(msg, ex);
                }
                else
                {
                    Logger.Fatal(msg, new Exception(e.ExceptionObject?.ToString()));
                }
                MessageWin.Error("系统发生未处理的异常", "系统错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "非UI线程发生未处理错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                Logger.Fatal("Task线程异常", e.Exception);
                e.SetObserved();//设置该异常已察觉（这样处理后就不会引起程序崩溃）
                MessageWin.Error("系统发生未处理的异常", "系统错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "非UI线程发生未处理错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                e.Handled = true; //把 Handled 属性设为true，表示此异常已处理，程序可以继续运行，不会强制退出
                Logger.Fatal("UI线程异常", e.Exception);
                MessageWin.Error("系统发生未处理的异常", "系统错误");
                if (MainWindow.Visibility != Visibility.Visible)
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UI线程发生未处理错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
