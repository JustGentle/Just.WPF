using Just.Base.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Just.Base
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowVM
    {
        #region 单例
        public Window MainWindow { get; set; }
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
            try
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
    'Id': 'MongoSync',
    'Parent': 'iOffice10',
    'Header': 'MongoDB同步',
    'ClassName': 'MongoSyncCtrl'
  },
  {
    'Id': 'VersionFile',
    'Parent': 'iOffice10',
    'Header': '版本文件生成',
    'ClassName': 'VersionFileCtrl'
  },
  {
    'Id': 'WebsiteMapper',
    'Parent': 'iOffice10',
    'Header': '站点文件映射*',
    'ClassName': 'WebsiteMapperCtrl'
  },
  {
    'Id': 'ConfigManager',
    'Parent': 'iOffice10',
    'Header': '配置文件管理*',
    'ClassName': 'ConfigManagerCtrl'
  },
  {
    'Id': 'ScriptManager',
    'Parent': 'iOffice10',
    'Header': '常用脚本管理*',
    'ClassName': 'ScriptManagerCtrl'
  },
  {
    'Id': 'ChangesetGetter',
    'Header': '变更集抽取*',
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
                var nodes = JsonConvert.DeserializeObject<List<MenuNode>>(json);
#else
                var nodes = ReadSetting(nameof(MainMenu), new List<MenuNode>());
#endif
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

        #region 静态方法
        /// <summary>
        /// 显示状态文本
        /// </summary>
        /// <param name="text"></param>
        public static void ShowStatus(string text = "就绪", bool isProcess = false, int process = 0)
        {
            Instance.StatusText = text;
            Instance.IsShowStatusProcess = isProcess;
            Instance.StatusProcess = process;
        }
        public static void DispatcherInvoke(Action action)
        {
            Instance.MainWindow.Dispatcher.Invoke(action);
        }
        public static TResult DispatcherInvoke<TResult>(Func<TResult> func)
        {
            return Instance.MainWindow.Dispatcher.Invoke(func);
        }

        private static readonly string _settingFile = AppDomain.CurrentDomain.BaseDirectory + @"\Setting.json";
        private static readonly JObject _setting = JsonConvert.DeserializeObject<JObject>(
            File.ReadAllText(_settingFile, Encoding.UTF8));
        private static Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        public static T ReadSetting<T>(string key, T defaultValue = default(T))
        {
            var token = _setting[key];
            var value = token == null ? defaultValue : token.ToObject<T>();
            return value;
        }
        public static void WriteSetting<T>(string key, T value)
        {
            _setting[key] = value == null ? null : JToken.FromObject(value);
        }
        public static void SaveSetting()
        {
            File.WriteAllText(_settingFile, _setting.ToString(), Encoding.UTF8);
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
