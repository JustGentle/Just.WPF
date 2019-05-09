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

namespace Just.WPF.Views.MongoDBTool
{ 
    /// <summary>
    /// MongoDBToolCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("MongoDB工具")]
    public partial class MongoDBToolCtrl : UserControl, IWriteSettings
    {
        private readonly MongoDBToolVM _vm = new MongoDBToolVM();
        public MongoDBToolCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.ReadSetting();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            WriteSettings();
        }
    }
}
