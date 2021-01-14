﻿using GenLibrary.GenControls;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Just.Base.Theme;
using Just.Base.Views;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace Just.MongoDB
{
    /// <summary>
    /// MongoSyncCtrl.xaml 的交互逻辑
    /// </summary>
    [DisplayName("MongoDB工具")]
    public partial class MongoSyncCtrl : UserControl, IChildView
    {
        private readonly MongoSyncVM _vm = new MongoSyncVM();
        private BraceFoldingStrategy _foldingStrategy { get; set; } = new BraceFoldingStrategy()
        {
            FoldingNameRegex = new Regex(@"(?<=""Display""\s*:\s*"").+(?="")", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
        };
        public MongoSyncCtrl()
        {
            InitializeComponent();
            this.DataContext = _vm;
            _vm.OnJsonChanged += _vm_OnJsonChanged;
            _vm.FindNextText += _vm_FindNextText;
        }
        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            codeEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E9CCA"));
            _foldingManager = _foldingManager ?? FoldingManager.Install(codeEditor.TextArea);
            txt.Focus();//使快捷键绑定生效
        }
        private void _vm_OnJsonChanged()
        {
            if(_foldingManager != null) _foldingStrategy.UpdateFoldings(_foldingManager, codeEditor.Document);
            codeEditor.CaretOffset = 0;
        }

        public void WriteSettings()
        {
            _vm.WriteSetting();
        }

        private void TreeListView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Right) return;
            if (sender is TreeListView tree)
            {
                //右键选中
                var p = e.GetPosition(tree);
                if (tree.InputHitTest(p) is DependencyObject item)
                {
                    if (VisualTreeHelperEx.FindAncestorByType(item, typeof(TreeListViewItem), true) is TreeListViewItem node)
                    {
                        node.IsSelected = true;
                        return;
                    }
                }
            }
        }

        private FoldingManager _foldingManager;
        private void ButtonOption_Click(object sender, RoutedEventArgs e)
        {
            OptionPopup.IsOpen = !OptionPopup.IsOpen;
        }

        private void MenuItemCollapse_Click(object sender, RoutedEventArgs e)
        {
            _foldingManager.AllFoldings.ToList().ForEach(f => f.IsFolded = true);
        }

        private void MenuItemExpand_Click(object sender, RoutedEventArgs e)
        {
            _foldingManager.AllFoldings.ToList().ForEach(f => f.IsFolded = false);
        }

        private int _vm_FindNextText(int start, string findText, bool previous)
        {
            var i = -1;
            if (string.IsNullOrEmpty(findText) || string.IsNullOrEmpty(codeEditor.Text)) return i;
            if (previous)
            {
                if (start == -1) start = codeEditor.SelectionStart;
                i = codeEditor.Text.LastIndexOf(findText, start);
            }
            else
            {
                if (start == -1) start = codeEditor.SelectionStart + codeEditor.SelectionLength;
                i = codeEditor.Text.IndexOf(findText, start);
            }
            if (i < 0) return i;
            codeEditor.Select(i, _vm.FindText.Length);
            codeEditor.ScrollToLine(codeEditor.Document.GetLineByOffset(i).LineNumber);
            return i;
        }

        public void ReadSettings(string[] args)
        {
            _vm.ReadSettings(args);
        }
    }
}
