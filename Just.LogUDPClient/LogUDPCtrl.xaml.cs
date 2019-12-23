using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Just.Base;
using Just.Base.Views;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml;

namespace Just.LogUDPClient
{
    /// <summary>
    /// LogUDPCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class LogUDPCtrl : UserControl, IChildView
    {
        private readonly LogUDPVM _vm;
        private FoldingManager _foldingManager;
        private XmlFoldingStrategy _foldingStrategy { get; set; } = new XmlFoldingStrategy();
        public LogUDPCtrl()
        {
            RegisterCustomHighlighting();
            InitializeComponent();
            _vm = new LogUDPVM();
            this.DataContext = _vm;
            _vm.ReadSetting();
            _vm.AfterWrite += _vm_AfterWrite;
        }
        private void RegisterCustomHighlighting()
        {
            // Load our custom highlighting definition
            IHighlightingDefinition customHighlighting;
            using (Stream s = this.GetType().Assembly.GetManifestResourceStream("Just.LogUDPClient.Resources.Custom.xshd"))
            {
                if (s == null)
                    throw new InvalidOperationException("Could not find embedded resource");
                using (XmlReader reader = new XmlTextReader(s))
                {
                    customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                        HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            // and register it in the HighlightingManager
            HighlightingManager.Instance.RegisterHighlighting("Custom", new string[] { ".xml" }, customHighlighting);
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            _foldingManager = _foldingManager ?? FoldingManager.Install(codeEditor.TextArea);
        }

        private void _vm_AfterWrite(object sender, RoutedEventArgs e)
        {
            if (_foldingManager == null) return;
            _foldingStrategy.UpdateFoldings(_foldingManager, codeEditor.Document);
            codeEditor.ScrollToEnd();
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
            _vm.StopListen.Execute(null);
        }
        private void MenuItemCollapse_Click(object sender, RoutedEventArgs e)
        {
            _foldingManager.AllFoldings.ToList().ForEach(f => f.IsFolded = true);
        }

        private void MenuItemExpand_Click(object sender, RoutedEventArgs e)
        {
            _foldingManager.AllFoldings.ToList().ForEach(f => f.IsFolded = false);
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new CommonSaveFileDialog($"日志另存为")
            {
                DefaultFileName = $"Log - {DateTime.Now:yyMMddHHmmssfff}",
                DefaultExtension = ".xml"
            };
            sfd.Filters.Add(new CommonFileDialogFilter("XML文件", "*.xml"));
            sfd.Filters.Add(new CommonFileDialogFilter("文本文件", "*.txt"));
            sfd.Filters.Add(new CommonFileDialogFilter("所有文件", "*.*"));
            if (sfd.ShowDialog(MainWindowVM.Instance.MainWindow) == CommonFileDialogResult.Ok)
            {
                var fileName = sfd.FileName;
                File.WriteAllBytes(fileName, Encoding.UTF8.GetBytes(codeEditor.Text));
                MainWindowVM.NotifyInfo("保存完成", "日志另存为");
            }
        }
    }
}
