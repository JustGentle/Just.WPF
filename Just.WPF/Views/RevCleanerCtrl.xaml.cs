using Just.WPF.ViewModels;
using System.Windows.Controls;

namespace Just.WPF.Views
{
    /// <summary>
    /// RevCleanerCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class RevCleanerCtrl : UserControl
    {
        private readonly RevCleanerSetting _vm;
        public RevCleanerCtrl()
        {
            InitializeComponent();
            _vm = RevCleanerSetting.Instance;
            this.DataContext = _vm;

            _vm.WebRootFolder = System.Environment.CurrentDirectory;
        }
    }
}
