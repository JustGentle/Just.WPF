using Just.Base;
using Just.Base.Views;
using Microsoft.TeamFoundation.VersionControl.Client;
using PropertyChanged;
using System.IO;

namespace Just.ExtractTfs
{
    [AddINotifyPropertyChangedInterface]
    public class SourceItemNode : NodeItemBase<SourceItemNode>
    {
        public SourceItemNode(Item item): base()
        {
            SourceItem = item;
        }
        public Item SourceItem { get; set; }
        public string ServerPath => SourceItem?.ServerItem;
        public string Name
        {
            get
            {
                if (SourceItem == null) return null;
                var name = Path.GetFileName(SourceItem.ServerItem);
                if (string.IsNullOrEmpty(name))
                    name = SourceItem.VersionControlServer.TeamProjectCollection.Name;
                return name;
            }

        }
        public bool IsFile => SourceItem?.ItemType == ItemType.File;
    }
}
