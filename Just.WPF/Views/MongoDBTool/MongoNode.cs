using PropertyChanged;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace Just.WPF.Views.MongoDBTool
{
    [AddINotifyPropertyChangedInterface]
    public class MongoNode
    {
        public string ImagePath { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public bool? IsEnable { get; set; }
        public string FileName { get; set; }
        public string UpdateTime { get; set; }
        public MongoNodeCollection Children { get; set; } = new MongoNodeCollection();
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
        public Brush Foreground { get; set; }

        public MongoNode this[string key]
        {
            get
            {
                return Children[key];
            }
        }
        public bool? SetEnableByChildren()
        {
            bool? result = null;
            if (Children.Any(c => c.IsEnable == true))
                result = true;
            if (Children.Any(c => c.IsEnable == false))
                if (result == true) result = null;
                else result = false;
            if (Children.Any(c => !c.IsEnable.HasValue))
                result = null;
            IsEnable = result;
            return result;
        }
    }

    public class MongoNodeCollection : ObservableCollection<MongoNode>
    {
        public MongoNodeCollection() : base()
        {

        }
        public MongoNodeCollection(List<MongoNode> list) : base(list)
        {

        }
        public MongoNodeCollection(IEnumerable<MongoNode> collection) : base(collection)
        {

        }
        public MongoNode this[string key]
        {
            get
            {
                return this?.FirstOrDefault(n => n.Key == key);
            }
        }
    }
}
