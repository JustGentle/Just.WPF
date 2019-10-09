using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Just.Base.Views
{
    /// <summary>
    /// ListWin.xaml 的交互逻辑
    /// </summary>
    public partial class ListWin : Window
    {
        public ListWin()
        {
            InitializeComponent();
            LayoutRoot.MaxWidth = SystemParameters.WorkArea.Width;
            LayoutRoot.MaxHeight = SystemParameters.WorkArea.Height;
            DataContext = this;
            Owner = MainWindowVM.Instance.MainWindow;
        }

        public object SelectedItem { get; set; }
        public bool HasItems => Items.EmptyIfNull().Any();
        public ObservableCollection<Tuple<string, object>> Items
        {
            get { return (ObservableCollection<Tuple<string, object>>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(ObservableCollection<Tuple<string, object>>), typeof(ListWin), new PropertyMetadata(null));



        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if(ListSelect.SelectedIndex == -1)
            {
                NotifyWin.Warn("未选择任何项！");
                return;
            }
            SelectedItem = ListSelect.SelectedValue;
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = null;
            this.DialogResult = false;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();//拖拽移动窗口
            e.Handled = true;
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Ok_Click(sender, e);
        }
    }
}
