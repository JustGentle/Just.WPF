using Just.Base.Views;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Client.Channels;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
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

namespace Just.ExtractTfs
{
    /// <summary>
    /// ExtractTfsCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class ExtractTfsCtrl : UserControl, IChildView
    {

        private readonly ExtractTfsVM _vm = new ExtractTfsVM();
        public ExtractTfsCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
        }

        public void ReadSettings(string[] args)
        {
            _vm.ReadSetting();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }
    }
}
