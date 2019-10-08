using Just.Base.Views;
using System.Windows.Controls;

namespace Just.WebsiteMklink
{
    /// <summary>
    /// WebsiteMklinkCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class WebsiteMklinkCtrl : UserControl, IChildView
    {
        private readonly WebsiteMklinkVM _vm = new WebsiteMklinkVM();
        public WebsiteMklinkCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.ReadSetting();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }
    }
}
