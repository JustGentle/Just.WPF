using PropertyChanged;
using System.Collections.ObjectModel;

namespace Just.WPF.Views.RevCleaner
{
    [AddINotifyPropertyChangedInterface]
    public class RevFileItem
    {
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string OrigFile { get; set; }
        public string RevFile { get; set; }
        public string UpdateTime { get; set; }
        public bool? IsKeep { get; set; }
        public bool IsFolder { get; set; }

        public ObservableCollection<RevFileItem> Children { get; set; } = new ObservableCollection<RevFileItem>();
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
    }
}
