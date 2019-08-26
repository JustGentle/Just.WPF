using Just.Base.Views;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Just.RegexNet
{
    /// <summary>
    /// RegexNetCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class RegexNetCtrl : UserControl, IChildView
    {
        private readonly RegexNetVM _vm = new RegexNetVM();
        public RegexNetCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
        }

        #region 选项
        private RegexOptions GetOptions()
        {
            RegexOptions options = RegexOptions.None;
            foreach (Border border in this.MenuOptions.Children)
            {
                if (!(border.Child is CheckBox item)) continue;
                if (item.IsChecked ?? false)
                {
                    var value = (RegexOptions)Enum.Parse(typeof(RegexOptions), item.Tag.ToString());
                    options = options | value;
                }
            }
            return options;
        }
        private void BorderOptionAll_MouseUp(object sender, MouseButtonEventArgs e)
        {
            foreach (Border border in this.MenuOptions.Children)
            {
                if (!(border.Child is CheckBox item)) continue;
                item.IsChecked = false;
            }
            OptionPopup.IsOpen = false;
        }
        #endregion

        #region 操作
        private void ButtonOption_Click(object sender, RoutedEventArgs e)
        {
            OptionPopup.IsOpen = !OptionPopup.IsOpen;
        }
        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            _vm.Reset();
            TextBox_Input.Select(0, 0);
        }
        private void ButtonIsMatch_Click(object sender, RoutedEventArgs e)
        {
            _vm.Init(GetOptions());
            _vm.IsMatch();
        }
        private void ButtonMatch_Click(object sender, RoutedEventArgs e)
        {
            _vm.Init(GetOptions());
            var match = _vm.Match();
            if (match?.Success ?? false)
            {
                TextBox_Input.Focus();
                TextBox_Input.Select(match.Index, match.Length);
            }
        }
        private void ButtonMatches_Click(object sender, RoutedEventArgs e)
        {
            _vm.Init(GetOptions());
            _vm.Matches();
        }
        private void ButtonReplace_Click(object sender, RoutedEventArgs e)
        {
            _vm.Init(GetOptions());
            _vm.Replace();
        }
        private void ButtonSplit_Click(object sender, RoutedEventArgs e)
        {
            _vm.Init(GetOptions());
            _vm.Split();
        }

        public void WriteSettings()
        {
        }

        #endregion
    }
}
