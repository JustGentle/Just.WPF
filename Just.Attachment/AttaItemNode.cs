using Just.Base.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Just.Attachment
{
    [AddINotifyPropertyChangedInterface]
    public class AttaItemNode: NodeItemBase<AttaItemNode>
    {
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(Path)) return string.Empty;
                return System.IO.Path.GetFileName(Path);
            }
        }
        public string Path { get; set; }
        public bool IsFolder { get; set; }
        public string UpdateTime { get; set; }
        public bool? IsKeep { get; set; }
        public int FileCount = 0;
        public int FolderCount = 0;

        public string UpdateCountInfo()
        {
            if (!IsFolder) return UpdateTime;
            UpdateTime = $"[{FileCount} 个文件，{FolderCount} 个文件夹]";
            return UpdateTime;
        }
    }
}
