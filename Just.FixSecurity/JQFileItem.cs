using Just.Base.Views;
using PropertyChanged;

namespace Just.FixSecurity
{
    [AddINotifyPropertyChangedInterface]
    public class JQFileItem : NodeItemBase<JQFileItem>
    {
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsFolder { get; set; }
        public string Version { get; set; }

        public int FileCount = 0;
        public void UpdateCountInfo()
        {
            if (!IsFolder) return;
            Version = $"[{FileCount} 个文件]";
        }
    }
}
