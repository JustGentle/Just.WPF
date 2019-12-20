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
using System.Reflection;
using System.Text;
using System.Windows;

namespace Just.Base
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowVM
    {
        public const string ResourcesBase = "/Just.MongoDB;Component";

        #region 单例
        public Window MainWindow { get; set; }
        private static MainWindowVM _Instance;
        public static MainWindowVM Instance
        {
            get
            {
                _Instance = _Instance ?? new MainWindowVM() { Title = ReadSetting(nameof(Title), "工具箱") };
                return _Instance;
            }
        }
        public MainWindowVM()
        {
            _Instance = this;
        }
        #endregion

        #region 属性
        public string Title { get; set; } = "工具箱";
        public ObservableCollection<MenuNode> MainMenu { get; set; } = new ObservableCollection<MenuNode>();
        public List<MenuNode> StartOn { get; set; } = new List<MenuNode>();
        public string StatusText { get; set; } = "就绪";
        public bool IsShowStatusProcess { get; set; } = false;
        public int StatusProcess { get; set; } = 0;
        public string VersionText { get; set; } = "版本：v1.0";
        public string VersionHint { get; set; }
        #endregion

        #region 菜单
        public void LoadMainMenu()
        {
            try
            {
#if DEBUG
                /*
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
    'Id': 'WebsiteMklink',
    'Parent': 'iOffice10',
    'Header': '站点文件映射',
    'ClassName': 'WebsiteMklinkCtrl'
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
                var nodes = JsonConvert.DeserializeObject<List<MenuNode>>(json);*/
                var nodes = ReadSetting(nameof(MainMenu), new List<MenuNode>());
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
        public void ShowVersion(Assembly assembly, string Title = "主程序")
        {
            var version = assembly.GetName().Version;
            VersionText = $"版本：v{version}";
            var baseTime = new DateTime(2000, 1, 1);
            var buildTime = TimeZone.CurrentTimeZone.ToLocalTime(baseTime.AddDays(version.Build).AddSeconds(version.Revision));
            VersionHint = $"{Title}\n生成时间：{buildTime:yyyy-MM-dd HH:mm:ss}";
        }
        #endregion

        #region DispatcherInvoke
        public static void DispatcherInvoke(Action action)
        {
            Instance.MainWindow.Dispatcher.Invoke(action);
        }
        public static TResult DispatcherInvoke<TResult>(Func<TResult> func)
        {
            return Instance.MainWindow.Dispatcher.Invoke(func);
        }
        #endregion

        #region Status
        private static DateTime _lastStatusTime;
        private static TimeSpan _statusTimeSpan = TimeSpan.MinValue;
        public static void BeginStatusInterval(long timeSpanMilliseconds = 500)
        {
            _statusTimeSpan = TimeSpan.FromMilliseconds(timeSpanMilliseconds);
        }
        public static void EndStatusInterval()
        {
            _statusTimeSpan = TimeSpan.MinValue;
        }
        /// <summary>
        /// 显示状态文本
        /// </summary>
        /// <param name="text"></param>
        public static void ShowStatus(string text = "就绪", bool isProcess = false, int? process = null)
        {
            var now = DateTime.Now;
            if (_statusTimeSpan != TimeSpan.MinValue && now - _lastStatusTime < _statusTimeSpan)
                return;
            _lastStatusTime = now;

            if(text != null) Instance.StatusText = text;
            Instance.IsShowStatusProcess = isProcess;
            if(process.HasValue) Instance.StatusProcess = process.Value;
        }
        #endregion

        #region Notify
        public static void NotifyInfo(string msg, string title = "提示")
        {
            DispatcherInvoke(() => { NotifyWin.Info(msg, title); });
        }
        public static void NotifyWarn(string msg, string title = "警告")
        {
            DispatcherInvoke(() => { NotifyWin.Warn(msg, title); });
        }
        public static void NotifyError(string msg, string title = "错误")
        {
            DispatcherInvoke(() => { NotifyWin.Error(msg, title); });
        }
        #endregion

        #region Message
        public static void MessageInfo(string msg, string title = "提示")
        {
            DispatcherInvoke(() => { MessageWin.Info(msg, title); });
        }
        public static void MessageWarn(string msg, string title = "警告")
        {
            DispatcherInvoke(() => { MessageWin.Warn(msg, title); });
        }
        public static void MessageError(string msg, string title = "错误")
        {
            DispatcherInvoke(() => { MessageWin.Error(msg, title); });
        }
        public static bool? MessageConfirm(string msg, string title = "确认")
        {
            return DispatcherInvoke(() => { return MessageWin.Confirm(msg, title); });
        }
        public static string MessageInput(string value = "", string msg = "", string title = "查找")
        {
            return DispatcherInvoke(() => { return MessageWin.Input(value, msg, title); });
        }
        #endregion

        #region Setting
        private static readonly string _settingFile = $@"{AppDomain.CurrentDomain.BaseDirectory}\{ConfigurationManager.AppSettings["setting"]}";
        private static readonly JObject _setting = JsonConvert.DeserializeObject<JObject>(
            File.ReadAllText(_settingFile, Encoding.UTF8));
        private static readonly string _defaultSettingFile = $@"{AppDomain.CurrentDomain.BaseDirectory}\{ConfigurationManager.AppSettings["default"]}";
        private static readonly JObject _defaultSetting = JsonConvert.DeserializeObject<JObject>(
            File.ReadAllText(_defaultSettingFile, Encoding.UTF8));

        public static T ReadSetting<T>(string key, T defaultValue = default(T))
        {
            var dtoken = _defaultSetting[key];
            var token = _setting[key] ?? dtoken;
            var value = token == null ? defaultValue : token.ToObject<T>();
            return value;
        }
        public static void WriteSetting<T>(string key, T value)
        {
            //不存在或与默认值不相同才写入
            if (_setting.ContainsKey(key) || JsonConvert.SerializeObject(_defaultSetting[key]) != JsonConvert.SerializeObject(value))
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
