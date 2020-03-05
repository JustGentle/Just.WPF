using Dapper;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Just.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using System.Xml;

namespace Just.Data
{
    /// <summary>
    /// SqlCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class SqlCtrl : UserControl, IDependency
    {
        public SqlCtrl()
        {
            RegisterCustomHighlighting();
            InitializeComponent();
            codeEditor.Document = new TextDocument("SELECT * FROM [dbo].[SysModuleVersion]");
        }
        private void RegisterCustomHighlighting()
        {
            // Load our custom highlighting definition
            IHighlightingDefinition customHighlighting;
            using (Stream s = this.GetType().Assembly.GetManifestResourceStream("Just.Data.Resources.Custom.xshd"))
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
            HighlightingManager.Instance.RegisterHighlighting("Custom", new string[] { ".sql" }, customHighlighting);
        }

        private void ButtonExec_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                tab.Items.Clear();
                using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(cnn.Text))
                {
                    using (var reader = connection.ExecuteReader(codeEditor.Document.Text))
                    {
                        while (!reader.IsClosed && reader.FieldCount > 0)
                        {
                            var table = new DataTable();
                            table.Load(reader);
                            tab.Items.Add(new TabItem
                            {
                                Header = $"{table.Rows.Count}行",
                                Content = new DataGrid
                                {
                                    AutoGenerateColumns = true,
                                    IsReadOnly = true,
                                    ItemsSource = table.AsDataView()
                                },
                                IsSelected = tab.Items.Count == 0
                            });
                        }
                        if (reader.RecordsAffected != -1)
                        {
                            var table = new DataTable();
                            table.Columns.Add(nameof(reader.RecordsAffected));
                            table.Rows.Add(reader.RecordsAffected);
                            tab.Items.Add(new TabItem
                            {
                                Header = $"{reader.RecordsAffected}行",
                                Content = new DataGrid
                                {
                                    AutoGenerateColumns = true,
                                    IsReadOnly = true,
                                    ItemsSource = table.AsDataView()
                                },
                                IsSelected = tab.Items.Count == 0
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindowVM.NotifyError(ex.Message);
            }
        }
    }
}
