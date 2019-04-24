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

namespace Just.WPF.Views
{
    /// <summary>
    /// WaitLayer.xaml 的交互逻辑
    /// </summary>
    public partial class WaitLayer : UserControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LayerText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(WaitLayer), new PropertyMetadata(""));

        
        public WaitLayer()
        {
            InitializeComponent();
            LayerGrid.DataContext = this;
        }
    }
}
