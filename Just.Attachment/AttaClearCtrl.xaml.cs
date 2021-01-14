using GenLibrary.GenControls;
using Just.Base.Theme;
using Just.Base.Views;
using System;
using System.Collections.Generic;
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

namespace Just.Attachment
{
    /// <summary>
    /// AttaClearCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class AttaClearCtrl : UserControl, IChildView
    {
        AttaClearVM _vm = new AttaClearVM();
        public AttaClearCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
        }

        public void ReadSettings(string[] args)
        {
            _vm.ReadSetting();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
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
    }
}
