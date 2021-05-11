using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
using Just.Base.Views;

namespace Just.MongoDB
{
    /// <summary>
    /// HeartbeatMsgCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("监测状态短信通知工具")]
    public partial class HeartbeatMsgCtrl : UserControl, IChildView
    {
        private readonly HeartbeatMsgVM _vm = new HeartbeatMsgVM();
        public HeartbeatMsgCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
        }

        public void ReadSettings(string[] args)
        {
            _vm.ReadSettings(args);
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }
    }
}
