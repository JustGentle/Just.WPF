using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Just.WPF.ViewModels
{
    public class RevFileItem
    {
        public string OrigFile { get; set; }
        public string RevFile { get; set; }
        public string CurFile { get; set; }
        public string UpdateTime { get; set; }
        public string IsKeep { get; set; }
    }
}
