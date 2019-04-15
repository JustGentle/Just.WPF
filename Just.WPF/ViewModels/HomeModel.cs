using Just.WPF.Views;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Just.WPF.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class HomeModel
    {
        #region 单例
        private static HomeModel _Instance;
        public static HomeModel Instance
        {
            get
            {
                _Instance = _Instance ?? new HomeModel();
                return _Instance;
            }
        }
        public HomeModel()
        {
            LoadMainMenu();
        }
        #endregion

        #region 属性
        public ObservableCollection<MenuNode> MainMenu { get; set; } = new ObservableCollection<MenuNode>();
        #endregion

        #region 方法
        public void LoadMainMenu()
        {
            var id = 0;
            MainMenu.Add(new MenuNode { Id = id, Header = "iOffice10" });
            MainMenu.Add(new MenuNode { Parent = id, Id = ++id, Header = "补丁文件清理", ClassName = nameof(RevCleanerCtrl) });
            //MainMenu.Add(new MenuNode { Id = ++id, Header = "TEST", ClassName = nameof(TestCtrl) });
        }
        public MenuNode GetMenuItem(int id)
        {
            return MainMenu.FirstOrDefault(e => e.Id == id);
        }
        public MenuNode GetMenuItem(string className)
        {
            return MainMenu.FirstOrDefault(e => e.ClassName == className);
        }
        #endregion
    }
    [AddINotifyPropertyChangedInterface]
    public class MenuNode
    {
        public int Id { get; set; }
        public int? Parent { get; set; }
        public string Header { get; set; }
        public string ClassName { get; set; }
    }
}
