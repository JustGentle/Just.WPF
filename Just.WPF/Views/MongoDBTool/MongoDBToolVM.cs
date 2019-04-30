using GenLibrary.MVVM.Base;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Just.WPF.Views.MongoDBTool
{
    [AddINotifyPropertyChangedInterface]
    public class MongoDBToolVM
    {

        public bool Doing { get; set; }
        public string JsonFolder { get; set; } = Directory.GetCurrentDirectory();
        public string JsFile { get; set; }
        public string Json { get; set; }
        public string Pattern { get; set; } = @"(?<=var data = \[).*?(?=\];)";
        public string Replacement { get; set; } = "{0}";

        private ICommand _JsonFolderBrowser;
        public ICommand JsonFolderBrowser
        {
            get
            {
                _JsonFolderBrowser = _JsonFolderBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog
                    {
                        IsFolderPicker = true
                    };

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.JsonFolder = dlg.FileName;
                    }
                });
                return _JsonFolderBrowser;
            }
        }

        private ICommand _JsFileBrowser;
        public ICommand JsFileBrowser
        {
            get
            {
                _JsFileBrowser = _JsFileBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog();

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.JsFile = dlg.FileName;
                    }
                });
                return _JsFileBrowser;
            }
        }

        private StringBuilder json;
        private ICommand _DoAction;
        public ICommand DoAction
        {
            get
            {
                _DoAction = _DoAction ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    Task.Run(() =>
                    {
                        MainWindow.Instance.ShowStatus("开始...");
                        Doing = true;
                        json = new StringBuilder();
                        Scan(JsonFolder);
                        Json = Format(json.ToString());
                        MainWindow.DispatcherInvoke(() => 
                        {
                            Clipboard.SetText(Json);
                            NotifyWin.Info("已复制到剪贴板", "合并完成");
                        });
                        Doing = false;
                        MainWindow.Instance.ShowStatus();
                    });
                });
                return _DoAction;
            }
        }

        private void Scan(string folder)
        {
            MainWindow.Instance.ShowStatus("扫描..." + folder);
            if (!Directory.Exists(folder))
            {
                MainWindow.DispatcherInvoke(() => { NotifyWin.Error("目录不存在：" + JsonFolder); });
                return;
            }
            var folders = Directory.GetDirectories(folder);
            foreach (var item in folders)
            {
                Scan(item);
            }
            var files = Directory.GetFiles(folder);
            foreach (var item in files)
            {
                var endcoding = EncodingGetter.GetType(item);
                var text = File.ReadAllText(item, endcoding);
                text = text.Trim();
                json.AppendLine(text);
            }
        }

        private string Format(string text)
        {
            //去除首尾空
            text = text.Trim();
            //去除多行注释
            text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
            //去除单行注释
            text = Regex.Replace(text, @"^\s*//[^\r\n]*$", "", RegexOptions.Multiline);
            //逗号分隔
            text = Regex.Replace(text, @"\}\s*\{", $@"}},{Environment.NewLine}{{");
            if (File.Exists(JsFile))
            {
                var endcoding = EncodingGetter.GetType(JsFile);
                var js = File.ReadAllText(JsFile, endcoding);
                text = Regex.Replace(js, Pattern, string.Format(Replacement, text));
            }
            return text;
        }

        
        public void ReadSetting()
        {
            JsonFolder = MainWindow.ReadSetting($"{nameof(MongoDBTool)}.{nameof(JsonFolder)}", JsonFolder);
            JsFile = MainWindow.ReadSetting($"{nameof(MongoDBTool)}.{nameof(JsFile)}", JsFile);
            Pattern = MainWindow.ReadSetting($"{nameof(MongoDBTool)}.{nameof(Pattern)}", Pattern);
            Replacement = MainWindow.ReadSetting($"{nameof(MongoDBTool)}.{nameof(Replacement)}", Replacement);
        }
        public void WriteSetting()
        {
            MainWindow.WriteSetting($"{nameof(MongoDBTool)}.{nameof(JsonFolder)}", JsonFolder);
            MainWindow.WriteSetting($"{nameof(MongoDBTool)}.{nameof(JsFile)}", JsFile);
            MainWindow.WriteSetting($"{nameof(MongoDBTool)}.{nameof(Pattern)}", Pattern);
            MainWindow.WriteSetting($"{nameof(MongoDBTool)}.{nameof(Replacement)}", Replacement);
        }
    }
}
