using System.Windows.Controls;

namespace Just.DistEditor
{
    /// <summary>
    /// DistEditorCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class DistEditorCtrl : UserControl, Base.Views.IChildView
    {
        private readonly DistEditorVM _vm = new DistEditorVM();
        public DistEditorCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.ReadSetting();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }

        private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //为了让快捷键生效
            this.txt.Focus();
        }
    }
}
