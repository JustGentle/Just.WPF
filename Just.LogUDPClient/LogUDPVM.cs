using GenLibrary.MVVM.Base;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using Just.Base;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace Just.LogUDPClient
{
    [AddINotifyPropertyChangedInterface]
    public class LogUDPVM
    {
        #region 属性
        public FoldingManager FoldingManager { get; set; }
        public XmlFoldingStrategy FoldingStrategy { get; set; } = new XmlFoldingStrategy();
        public TextDocument Document { get; set; } = new TextDocument("<log>" + Environment.NewLine);

        public int Port { get; set; } = 8770;
        public bool Listening { get; set; } = false;
        #endregion

        #region 事件
        public event RoutedEventHandler AfterWrite;
        #endregion

        #region 监听
        private DateTime _startTime;
        private ICommand _StartListen;
        public ICommand StartListen
        {
            get
            {
                _StartListen = _StartListen ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        m_server_socket = new Socket(AddressFamily.InterNetwork,
                            SocketType.Dgram, ProtocolType.Udp);
                        IPEndPoint server = new IPEndPoint(IPAddress.Any, Port);
                        m_server_socket.Bind(server); //绑定udp指定端口
                        IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
                        m_epSender = client;
                        //异步通讯方式接受udp数据
                        m_server_socket.BeginReceiveFrom(m_recive_data, 0,
                            m_recive_data.Length, SocketFlags.None,
                                ref m_epSender, new AsyncCallback(ReceiveData), m_epSender);
                        Listening = true;
                        _startTime = DateTime.Now;
                        WriteLine("<Listen port=\"{0}\">\n  <start time=\"{1:yyyy-MM-dd HH:mm:ss.fffffff}\" />", Port, _startTime);
                    }
                    catch (Exception ex)
                    {
                        var error = $"无法监听 UDP 端口 {Port}.";
                        Logger.Error(error, ex);
                        WriteError(error, ex);
                        MainWindowVM.NotifyError(error);
                    }
                });
                return _StartListen;
            }

        }

        private ICommand _StopListen;
        public ICommand StopListen
        {
            get
            {
                _StopListen = _StopListen ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        if (!Listening) return;
                        Listening = false;
                        if (m_server_socket == null) return;
                        m_server_socket.Close();
                        m_server_socket.Dispose();
                        var endTime = DateTime.Now;
                        WriteLine("  <end time=\"{0:yyyy-MM-dd HH:mm:ss.fffffff}\" duration=\"{1}\" />\n</Listen>", endTime, endTime - _startTime);
                        MainWindowVM.NotifyWarn($"已停止监听端口 {Port}");
                    }
                    catch (Exception ex)
                    {
                        var error = $"无法停止监听 UDP 端口 {Port}.";
                        Logger.Error(error, ex);
                        WriteError(error, ex);
                        MainWindowVM.NotifyError(error);
                    }
                });
                return _StopListen;
            }
        }

        //解析日志格式的正则表达式
        private readonly Regex m_log_re = new Regex(@"(?<=</?)\w+:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private Socket m_server_socket; //监听端口返回的socket
        private EndPoint m_epSender; //接受数据的客户端套接字信息
        private byte[] m_recive_data = new byte[65536]; //udp数据接受缓冲区
        private void ReceiveData(IAsyncResult ar)
        {
            int recv_len = 0;
            try
            {
                if (!Listening) return;
                recv_len = m_server_socket.EndReceiveFrom(ar, ref m_epSender);
                //对接受的数据进行utf8转码
                string recv_text = Encoding.UTF8.GetString(m_recive_data, 0, recv_len);
                OnAppendLog(recv_text); //调用日志的控制台输出函数

                if (m_server_socket != null) //再次启动异步回调接受udp数据
                    m_server_socket.BeginReceiveFrom(m_recive_data, 0, m_recive_data.Length,
                        SocketFlags.None, ref m_epSender, new AsyncCallback(ReceiveData), m_epSender);
            }
            catch (Exception ex)
            {
                Logger.Error("接收数据错误", ex);
                WriteError("接收数据错误", ex);
            }
        }
        private void OnAppendLog(string logMsg)
        {
            logMsg = m_log_re.Replace(logMsg, string.Empty);
            try
            {
                logMsg = XElement.Parse(logMsg).ToString();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.ToString());
            }
            WriteLine(logMsg);
        }
        #endregion

        #region 输出
        private void WriteLine(string text, params object[] args)
        {
            Write(text, args);
            MainWindowVM.DispatcherInvoke(() => Document.Insert(Document.TextLength, Environment.NewLine));
        }
        private void Write(string text, params object[] args)
        {
            MainWindowVM.DispatcherInvoke(() =>
            {
                if (args?.Any() ?? false)
                    Document.Insert(Document.TextLength, string.Format(text, args));
                else
                    Document.Insert(Document.TextLength, text);

                AfterWrite?.Invoke(null, null);
            });
        }
        private void WriteError(string msg, Exception ex)
        {
            var xml = new XElement("event");
            xml.SetAttributeValue("level", "ERROR");
            xml.Add(new XElement("message") { Value = msg });
            xml.Add(new XElement("exception") { Value = ex.ToString() });
            WriteLine(xml.ToString());
        }
        #endregion


        #region Setting
        public void ReadSetting()
        {
            Port = MainWindowVM.ReadSetting($"{nameof(LogUDPCtrl)}.{nameof(Port)}", Port);
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(LogUDPCtrl)}.{nameof(Port)}", Port);
        }
        #endregion
    }
}
