using GenLibrary.GenControls;
using Just.Base.Theme;
using Just.Base.Views;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Just.MongoDB
{
    /// <summary>
    /// MongoSyncCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("MongoDB工具")]
    public partial class MongoSyncCtrl : UserControl, IChildView
    {
        private readonly MongoSyncVM _vm = new MongoSyncVM();
        public MongoSyncCtrl()
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

        private void TreeListView_MouseDown(object sender, MouseButtonEventArgs e)
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

        private void ButtonOption_Click(object sender, RoutedEventArgs e)
        {
            OptionPopup.IsOpen = !OptionPopup.IsOpen;
        }
    }
}
