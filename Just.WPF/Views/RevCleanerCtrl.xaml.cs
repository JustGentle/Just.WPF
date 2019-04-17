using Just.WPF.ViewModels;
using System.Windows.Controls;

namespace Just.WPF.Views
{
    /// <summary>
    /// RevCleanerCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class RevCleanerCtrl : UserControl
    {
        private readonly RevCleanerSetting _vm = new RevCleanerSetting();
        public RevCleanerCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
        }
    }
}
