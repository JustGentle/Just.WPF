using ICSharpCode.AvalonEdit.Document;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Just.Data
{
    [AddINotifyPropertyChangedInterface]
    public class SqlVM
    {
        public TextDocument Document { get; set; } = new TextDocument("SELECT TOP (1000) * FROM [iOffice10].[dbo].[ArcFile]");
    }
}
