using GenLibrary.GenControls;
using Just.Base.Theme;
using Just.Base.Views;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Just.Rev
{
    /// <summary>
    /// RevCleanerCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("补丁文件清理")]
    public partial class RevCleanerCtrl : UserControl, IChildViews
    {
        private readonly RevCleanerVM _vm = new RevCleanerVM();
        public RevCleanerCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.ReadSetting();
        }

        private void TreeListView_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != System.Windows.Input.MouseButton.Right) return;
            if (sender is TreeListView tree)
            {
                //右键选中
                var p = e.GetPosition(tree);
                if (tree.InputHitTest(p) is DependencyObject item)
                {
                    if (VisualTreeHelperEx.FindAncestorByType(item, typeof(TreeListViewItem), true) is TreeListViewItem node)
                    {
                        node.IsSelected = true;
                        return;
                    }
                }
            }
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            WriteSettings();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }
    }
}
