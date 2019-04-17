using System.Collections.ObjectModel;

namespace Just.WPF.ViewModels
{
    public class RevFileItem
    {
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string OrigFile { get; set; }
        public string RevFile { get; set; }
        public string UpdateTime { get; set; }
        public bool IsKeep { get; set; }

        public ObservableCollection<RevFileItem> Children { get; private set; } = new ObservableCollection<RevFileItem>();
        public bool IsExpanded { get; set; }
    }
}
