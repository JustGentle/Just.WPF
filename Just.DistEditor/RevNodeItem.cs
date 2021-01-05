using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Just.Base.Views;

namespace Just.DistEditor
{
    public class RevNodeItem : NodeItemBase<RevNodeItem>
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsFile { get; set; }
    }
}
