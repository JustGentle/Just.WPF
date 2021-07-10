using GenLibrary.GenControls;
using Just.Base.Theme;
using Just.Base.Views;
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

namespace Just.FixSecurity
{
    /// <summary>
    /// JQueryVerCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("JQuery版本替换")]
    public partial class JQueryVerCtrl : UserControl, IChildView
    {
        private readonly JQueryVerVM _vm = new JQueryVerVM();
        public JQueryVerCtrl()
        {
            InitializeComponent();
            DataContext = _vm;
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

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }

        public void ReadSettings(string[] args)
        {
            _vm.ReadSetting();
        }
    }
}
