using Just.Base.Views;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Just.WebService
{
    [AddINotifyPropertyChangedInterface]
    public class TemplateNode : NodeItemBase<TemplateNode>
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public string ParentField { get; set; }
        public TemplateNode Parent { get; set; }
    }
}
