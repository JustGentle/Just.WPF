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

namespace Just.Data
{
    /// <summary>
    /// i07SealImg.xaml 的交互逻辑
    /// </summary>
    public partial class i07SealImg : UserControl, IChildView
    {
        private readonly i07SealImgVM _vm = new i07SealImgVM();
        public i07SealImg()
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox)?.ScrollToEnd();
        }
    }
}
