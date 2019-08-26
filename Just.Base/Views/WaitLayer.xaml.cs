using System.Windows;
using System.Windows.Controls;

namespace Just.Base.Views
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
