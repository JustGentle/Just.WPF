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

namespace Just.WebService
{
    /// <summary>
    /// i09ScienceCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class i09ScienceCtrl : UserControl, Just.Base.Views.IChildView
    {
        private readonly i09ScienceVM _vm = new i09ScienceVM();
        public i09ScienceCtrl()
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
