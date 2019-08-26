using PropertyChanged;
using System.Collections.ObjectModel;

namespace Just.Rev
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


        public int FileCount = 0;
        public int FolderCount = 0;
        public void UpdateCountInfo()
        {
            if (!IsFolder) return;
            OrigFile = $"[{FileCount} 个文件]";
            RevFile = $"[{FolderCount} 个文件夹]";
        }
    }
}
