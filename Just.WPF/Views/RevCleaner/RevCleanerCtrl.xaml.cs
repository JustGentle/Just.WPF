using System.ComponentModel;
using System.Windows.Controls;

namespace Just.WPF.Views.RevCleaner
{
    /// <summary>
    /// RevCleanerCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("补丁文件清理")]
    public partial class RevCleanerCtrl : UserControl
    {
        private readonly RevCleanerVM _vm = new RevCleanerVM();
        public RevCleanerCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.ReadSetting();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _vm.WriteSetting();
        }
    }
}
