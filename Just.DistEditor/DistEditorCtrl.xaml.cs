using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GenLibrary.GenControls;
using Just.Base.Theme;

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

        private void ItemTree_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
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

        public void ReadSettings(string[] args)
        {
            _vm.ReadSetting();
        }
    }
}
