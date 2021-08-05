using Dapper;
using GenLibrary.MVVM.Base;
using Just.Base;
using Microsoft.WindowsAPICodePack.Dialogs;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Just.WebService
{

    [AddINotifyPropertyChangedInterface]
    public class i09ScienceVM
    {
        public string ConnectionString09 { get; set; } = "Data Source =.; Initial Catalog = iOffice; User ID = sa; Password = asdASD!@#;";
        public string ServiceUrl { get; set; } = "http://127.0.0.1/Areas/WorkFlow/Content/Svc/wssWorkFlow.asmx";
        public string FilePath { get; set; }
        public bool Doing { get; set; }
        public TemplateNode Tree { get; set; }

        #region WorkFlowData
        public string UserCode { get; set; }
        public string UserLoginName { get; set; }
        public string CreateUserDepCodes { get; set; }
        public string TemplateIdOrCode { get; set; }
        public string TemplateTitle { get; set; }
        public bool bSumitFlag { get; set; } = true;
        #endregion

        #region Template
        private ICommand _FileBrowser;
        public ICommand FileBrowser
        {
            get
            {
                _FileBrowser = _FileBrowser ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    var dlg = new CommonOpenFileDialog();
                    dlg.Filters.Add(new CommonFileDialogFilter("表单模板", "*.xml"));

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        this.FilePath = dlg.FileName;
                        ReadTemplate();
                    }
                });
                return _FileBrowser;
            }
        }

        private TemplateNode ReadTemplate()
        {
            Tree = new TemplateNode();
            if (!File.Exists(FilePath))
            {
                MainWindowVM.NotifyWarn("表单模板不存在：" + FilePath);
                return Tree;
            }
            var xml = XElement.Load(FilePath);
            var myField = xml.XPathSelectElement("/DataMappings/DataMapping/myField");
            if(myField == null)
            {
                MainWindowVM.NotifyWarn("表单模板找不到字段定义");
                return Tree;
            }
            ReadTemplateNode(myField, Tree);
            return Tree;
        }
        const string TableNodeType = "Table";
        private TemplateNode ReadTemplateNode(XElement node, TemplateNode tree)
        {
            var data = new TemplateNode
            {
                Name = node.Name.LocalName
            };
            var tableName = node.Attribute("TableName")?.Value;
            var fieldName = node.Attribute("FieldName")?.Value;
            var type = node.Attribute("type")?.Value;
            var length = node.Attribute("length")?.Value;
            var parentGroup = node.Attribute("ParentGroup")?.Value;

            data.Code = tableName ?? fieldName;
            data.Type = type;
            if(data.Type == null)
            {
                data.Type = TableNodeType;
            }
            else if(!string.IsNullOrEmpty(length))
            {
                data.Type += $"({length})";
            }

            if (string.IsNullOrEmpty(parentGroup))
            {
                var parent = GetTableNodeByName(parentGroup, Tree);
                data.Parent = parent;
                if(parent != null)
                {
                    parent?.Children.Add(data);
                }
                else
                {
                    tree.Children.Add(data);
                }
            }
            else
            {
                tree.Children.Add(data);
            }

            var items = node.Elements();
            if (items.Any())
            {
                foreach (var item in node.Elements())
                {
                    ReadTemplateNode(item, data);
                }
            }

            return data;
        }
        private TemplateNode GetTableNodeByName(string name, TemplateNode tree)
        {
            if (tree.Type == TableNodeType && string.Equals(tree.Name, name))
                return tree;
            if (tree.Children == null)
                return null;
            foreach (var node in tree.Children)
            {
                var result = GetTableNodeByName(name, node);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion

        #region Do
        private ICommand _DoAction;
        public ICommand DoAction
        {
            get
            {
                _DoAction = _DoAction ?? new RelayCommand<RoutedEventArgs>(_ =>
                {
                    try
                    {
                        if (Doing)
                        {
                            tokenSource?.Cancel();
                            MainWindowVM.ShowStatus("停止...");
                        }
                        else
                        {
                            Doing = true;
                            Do();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("科研数据迁移错误", ex);
                        MainWindowVM.NotifyError("执行错误：" + ex.Message);
                    }
                    finally
                    {
                        Doing = false;
                    }
                });
                return _DoAction;
            }
        }

        private CancellationTokenSource tokenSource;
        private void Do()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString09))
            {
                MainWindowVM.NotifyWarn("请先配置09链接字符串");
                return;
            }
            if(string.IsNullOrWhiteSpace(UserCode) && string.IsNullOrWhiteSpace(UserLoginName))
            {
                MainWindowVM.NotifyWarn("请先配置工号或账号");
                return;
            }
            if (string.IsNullOrWhiteSpace(TemplateIdOrCode))
            {
                MainWindowVM.NotifyWarn("请先配置模板ID或编码");
                return;
            }
            var workflowData = new XElement("workflowData");
            workflowData.Add(new XElement("userCode", UserCode));
            workflowData.Add(new XElement("userLoginName", UserLoginName));
            workflowData.Add(new XElement("createUserDepCodes", CreateUserDepCodes));
            workflowData.Add(new XElement("templateIdOrCode", TemplateIdOrCode));
            workflowData.Add(new XElement("templateTitle", TemplateTitle));
            workflowData.Add(new XElement("bSumitFlag", bSumitFlag));

            var formData = new XElement("myField");
            var caller = new Just.Base.Utils.WebServiceCaller(ServiceUrl);
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(ConnectionString09))
            {
                var topNode = Tree.Children.First();
                if (string.IsNullOrWhiteSpace(topNode.Data))
                {
                    MainWindowVM.NotifyWarn("未配置主表关联数据");
                    return;
                }
                var mains = connection.Query($"SELECT * FROM dbo.[{topNode.Data}]").Cast<IDictionary<string, object>>();
                foreach (var main in mains)
                {
                    var parentId = main["ID"];
                    foreach (var field in topNode.Children)
                    {
                        if (string.IsNullOrWhiteSpace(field.Data) || string.IsNullOrWhiteSpace(field.ParentField))
                            continue;
                        if (field.Type == TableNodeType)
                        {
                            var table = new XElement(field.Name);
                            formData.Add(table);
                            var details = connection.Query($"SELECT * FROM dbo.[{field.Data}] WHERE [{field.ParentField}] = {parentId}").Cast<IDictionary<string, object>>();
                            foreach (var detail in details)
                            {
                                var row = new XElement("item");
                                foreach (var item in field.Children)
                                {
                                    var element = new XElement(item.Name);
                                    row.Add(element);

                                    if (string.IsNullOrWhiteSpace(item.Data))
                                        continue;
                                    if (detail.ContainsKey(item.Data))
                                    {
                                        var value = detail[item.Data]?.ToString();
                                        if (value != null)
                                            element.Value = value;
                                    }
                                }
                                table.Add(row);
                            }
                        }
                        else
                        {
                            var element = new XElement(field.Name);
                            formData.Add(element);

                            if (main.ContainsKey(field.Data))
                            {
                                var value = main[field.Data]?.ToString();
                                if (value != null)
                                    element.Value = value;
                            }
                        }
                    }
                }
            }
            var result = caller.InvokeWebService("StartOAWorkFlow", new List<XElement> { 
                new XElement("workflowData", workflowData.ToString())
                , new XElement("formData", formData.ToString())
            });
            Logger.Debug(result.OuterXml);
            MainWindowVM.NotifyInfo(result.InnerText);
        }
        #endregion

        #region Setting
        public void ReadSetting()
        {
            ConnectionString09 = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(ConnectionString09)}", ConnectionString09);
            ServiceUrl = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(ServiceUrl)}", ServiceUrl);
            UserCode = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(UserCode)}", UserCode);
            UserLoginName = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(UserLoginName)}", UserLoginName);
            CreateUserDepCodes = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(CreateUserDepCodes)}", CreateUserDepCodes);
            TemplateIdOrCode = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(TemplateIdOrCode)}", TemplateIdOrCode);
            TemplateTitle = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(TemplateTitle)}", TemplateTitle);
            bSumitFlag = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(bSumitFlag)}", bSumitFlag);
            Tree = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(Tree)}", Tree);
            /*
            FilePath = MainWindowVM.ReadSetting($"{nameof(i09ScienceCtrl)}.{nameof(FilePath)}", FilePath);
            if (!string.IsNullOrEmpty(FilePath))
            {
                ReadTemplate();
            }
            */
        }
        public void WriteSetting()
        {
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(ConnectionString09)}", ConnectionString09);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(ServiceUrl)}", ServiceUrl);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(UserCode)}", UserCode);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(UserLoginName)}", UserLoginName);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(CreateUserDepCodes)}", CreateUserDepCodes);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(TemplateIdOrCode)}", TemplateIdOrCode);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(TemplateTitle)}", TemplateTitle);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(bSumitFlag)}", bSumitFlag);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(FilePath)}", FilePath);
            MainWindowVM.WriteSetting($"{nameof(i09ScienceCtrl)}.{nameof(Tree)}", Tree);
        }
        #endregion
    }
}
