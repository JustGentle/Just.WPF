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
            _vm.LogAppended += _vm_LogAppended;
            _vm.ReadSetting();
        }

        private void _vm_LogAppended(object sender, System.EventArgs e)
        {
            if (TextBoxLog.IsFocused) return;
            TextBoxLog.ScrollToEnd();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }
    }
}
