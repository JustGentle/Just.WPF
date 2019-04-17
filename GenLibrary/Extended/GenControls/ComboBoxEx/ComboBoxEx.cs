using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
namespace GenLibrary.GenControls
{
    /// <summary>
    /// ComboBox控件的扩展
    /// </summary>
    public class ComboBoxEx : ComboBox
    {
        #region 下拉框内容绑定
        /// <summary>
        /// Identifies the BackgroundContent dependency property.
        /// </summary>
        public static readonly DependencyProperty DropListControlProperty = DependencyProperty.Register("DropListControl", typeof(ItemsControl), typeof(ComboBoxEx));
        /// <summary>
        /// 获取或者设置下拉列表控件
        /// </summary>
        public ItemsControl DropListControl
        {
            get { return (ItemsControl)GetValue(DropListControlProperty); }
            set { SetValue(DropListControlProperty, value); }
        }
        #endregion
        static ComboBoxEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ComboBoxEx), new FrameworkPropertyMetadata(typeof(ComboBoxEx)));
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (DropListControl == null) return;
            Binding ItemSourceBinding = new Binding();
           
                //设置绑定的源控件  
                ItemSourceBinding.Source = this;
                //设置要绑定属性  
                ItemSourceBinding.Path = new PropertyPath("ItemsSource");
                //设置绑定到要绑定的控件 ，相当于DropListControl绑定到ComboBox
                DropListControl.SetBinding(ComboBox.ItemsSourceProperty, ItemSourceBinding);
            
            

            Binding SelectedItemBinding = new Binding();
            //设置绑定的源控件  
            SelectedItemBinding.Source = this;
            SelectedItemBinding.Mode = BindingMode.TwoWay;
            SelectedItemBinding.NotifyOnSourceUpdated = true;
            //设置要绑定属性  
            SelectedItemBinding.Path = new PropertyPath("SelectedItem");
            //设置绑定到要绑定的控件,相当于DropListControl绑定到ComboBox
            DropListControl.SetBinding(ComboBox.SelectedItemProperty, SelectedItemBinding);
        }
        //当选择后，关闭下拉列表
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }
        }
        //展开时，自动滚动到选中位置
        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            if (DropListControl is DataGrid)
            {

                DataGrid dt = DropListControl as DataGrid;
                if(dt.SelectedItem!=null)
                   dt.ScrollIntoView(dt.SelectedItem);

            }
            else if (DropListControl is ListView)
            {
                ListView dt = DropListControl as ListView;
                if (dt.SelectedItem != null)
                    dt.ScrollIntoView(dt.SelectedItem);
            }
            else if (DropListControl is ListBox)
            {
                ListBox dt = DropListControl as ListBox;

                dt.ScrollIntoView(dt.SelectedItem);
            }
            else if (DropListControl is TreeListView)
            {
                TreeListView dt = DropListControl as TreeListView;
                //dt.ScrollIntoView(dt.SelectedItem);
            }
        }
    }
}//end namespace
