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
            ListSelect.MaxHeight = LayoutRoot.MaxHeight - 38 - 50;
            DataContext = this;
            Owner = MainWindowVM.Instance.MainWindow;
        }

        public List<object> SelectedItems { get; set; } = new List<object>();
        public object SelectedItem { get; set; }
        public bool HasItems => Items.EmptyIfNull().Any();
        private string _filter;
        public string Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
                if (string.IsNullOrEmpty(value))
                {
                    SetValue(ItemsProperty, new ObservableCollection<Tuple<string, object>>(_items));
                }
                else
                {
                    var items = Items.Where(i => i.Item1?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false);
                    SetValue(ItemsProperty, new ObservableCollection<Tuple<string, object>>(items));
                }
            }
        }

        private IEnumerable<Tuple<string, object>> _items;
        public IEnumerable<Tuple<string, object>> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                SetValue(ItemsProperty, new ObservableCollection<Tuple<string, object>>(value));
            }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(ObservableCollection<Tuple<string, object>>), typeof(ListWin), new PropertyMetadata(null));



        public bool MultiSelect
        {
            get { return (SelectionMode)GetValue(MultiSelectProperty) != SelectionMode.Single; }
            set { SetValue(MultiSelectProperty, value ? SelectionMode.Extended : SelectionMode.Single); }
        }

        // Using a DependencyProperty as the backing store for MultiSelect.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MultiSelectProperty =
            DependencyProperty.Register("MultiSelect", typeof(SelectionMode), typeof(ListWin), new PropertyMetadata(SelectionMode.Single));


        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ListSelect.SelectedIndex == -1)
            {
                NotifyWin.Warn("未选择任何项！");
                return;
            }
            SelectedItem = ListSelect.SelectedValue;
            SelectedItems = ListSelect.SelectedItems.Cast<Tuple<string, object>>().Select(v => v.Item2).ToList();
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = null;
            SelectedItems = new List<object>();
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

        /// <summary>
        /// 最大化/还原
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WinStateMenuItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SwitchWindowState();
        }
        private void SwitchWindowState()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                SizeToContent = SizeToContent.Manual;
                this.WindowState = WindowState.Maximized;
            }
        }
    }
}
