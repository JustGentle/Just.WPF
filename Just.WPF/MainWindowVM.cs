using Just.WPF.Views;
using Just.WPF.Views.MongoDBTool;
using Just.WPF.Views.RevCleaner;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Just.WPF
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowVM
    {
        #region 单例
        private static MainWindowVM _Instance;
        public static MainWindowVM Instance
        {
            get
            {
                _Instance = _Instance ?? new MainWindowVM();
                return _Instance;
            }
        }
        public MainWindowVM()
        {
            LoadMainMenu();
        }
        #endregion

        #region 属性
        public ObservableCollection<MenuNode> MainMenu { get; set; } = new ObservableCollection<MenuNode>();
        public List<MenuNode> StartOn { get; set; } = new List<MenuNode>();
        public string StatusText { get; set; } = "就绪";
        public bool IsShowStatusProcess { get; set; } = false;
        public int StatusProcess { get; set; } = 0;
        #endregion

        #region 方法
        public void LoadMainMenu()
        {
            var iOffice10 = new MenuNode { Id = Guid.NewGuid(), Header = "iOffice10" };
            MainMenu.Add(iOffice10);
            NewMenuItem(typeof(RevCleanerCtrl), iOffice10);
            NewMenuItem(typeof(MongoDBToolCtrl), iOffice10, true);
            NewMenuItem(typeof(ChangesetGetterCtrl), visible: false);
        }
        private void NewMenuItem(Type view, MenuNode parent = null, bool startOn = false, bool visible = true)
        {
            var menu = new MenuNode
            {
                Parent = parent?.Id,
                Id = Guid.NewGuid(),
                ClassName = view.Name,
                Header = view.ToDisplayName(),
                Visible = visible
            };
            MainMenu.Add(menu);
            if (startOn) StartOn.Add(menu);
        }
        public MenuNode GetMenuItem(Guid id)
        {
            return MainMenu.FirstOrDefault(e => e.Id == id);
        }
        public MenuNode GetMenuItem(string className)
        {
            return MainMenu.FirstOrDefault(e => e.ClassName == className);
        }
        public MenuNode GetMenuItem(Type view)
        {
            return MainMenu.FirstOrDefault(e => e.ClassName == view.Name);
        }

        #endregion
    }
    [AddINotifyPropertyChangedInterface]
    public class MenuNode
    {
        public Guid Id { get; set; }
        public Guid? Parent { get; set; }
        public string Header { get; set; }
        public string ClassName { get; set; }
        public bool Visible { get; set; } = true;
    }
}
