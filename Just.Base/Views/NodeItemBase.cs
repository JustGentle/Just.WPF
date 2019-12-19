using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Just.Base.Views
{
    public abstract class NodeItemBase<T>
    {
        public virtual bool IsExpanded { get; set; }
        public virtual bool IsSelected { get; set; }
        public virtual Brush Foreground { get; set; }
        public virtual ObservableCollection<T> Children { get; set; } = new ObservableCollection<T>();
    }
}
