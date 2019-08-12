using Just.WPF.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

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
#if DEBUG
            var json = @"
[
  {
    'Id': 'iOffice10',
    'Header': 'iOffice10'
  },
  {
    'Id': 'RevCleaner',
    'Parent': 'iOffice10',
    'Header': '补丁文件清理',
    'ClassName': 'RevCleanerCtrl'
  },
  {
    'Id': 'MongoDBSync',
    'Parent': 'iOffice10',
    'Header': 'MongoDB同步',
    'ClassName': 'MongoDBToolCtrl'
  },
  {
    'Id': 'ConfigManager',
    'Parent': 'iOffice10',
    'Header': '配置文件管理',
    'ClassName': 'ConfigManagerCtrl'
  },
  {
    'Id': 'WebsiteMapper',
    'Header': '站点文件映射',
    'ClassName': 'WebsiteMapperCtrl'
  },
  {
    'Id': 'ChangesetGetter',
    'Header': '变更集抽取',
    'ClassName': 'ChangesetGetterCtrl',
    'Visible': true
  },
  {
    'Id': 'RegexNet',
    'Header': '正则表达式',
    'ClassName': 'RegexNetCtrl'
  }
]
";
#else
            var json = MainWindow.ReadSetting(nameof(MainMenu));
#endif
            Logger.Debug(json);
            try
            {
                var nodes = JsonConvert.DeserializeObject<List<MenuNode>>(json ?? string.Empty) ?? new List<MenuNode>();
                foreach (var item in nodes)
                {
                    MainMenu.Add(item);
                }
                if (!MainMenu.Any())
                {
                    NotifyWin.Warn("主菜单无数据");
                }
                else
                {
                    StartOn = MainMenu.Where(m => m.StartOn).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("加载主菜单错误", ex);
                NotifyWin.Error("加载主菜单错误:" + ex.Message);
            }
        }
        public MenuNode GetMenuItem(string id)
        {
            return MainMenu.FirstOrDefault(e => e.Id == id);
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
        public string Id { get; set; }
        public string Parent { get; set; }
        public string Header { get; set; }
        public string ClassName { get; set; }
        public bool Visible { get; set; } = true;
        public bool StartOn { get; set; }
    }
}
