using System;
using System.Windows.Input;
namespace GenLibrary.MVVM.Base
{
    /// <summary> 
    /// Delegatecommand，这种WPF.SL都可以用，VIEW里面直接使用INTERACTION的trigger激发,适合不同的UIElement控件 
    /// </summary> 
    public class DelegateCommand : ICommand
    {
        Func<object, bool> canExecute;
        Action<object> executeAction;
        bool canExecuteCache;
        public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecute)
        {
            this.executeAction = executeAction;
            this.canExecute = canExecute;
        }
        #region ICommand Members
        public bool CanExecute(object parameter)
        {
            bool temp = canExecute(parameter);
            if (canExecuteCache != temp)
            {
                canExecuteCache = temp;
                if (CanExecuteChanged != null)
                {
                    CanExecuteChanged(this, new EventArgs());
                }
            }
            return canExecuteCache;
        }
        public event EventHandler CanExecuteChanged;
        public void Execute(object parameter)
        {
            executeAction(parameter);
        }
        #endregion
    }
}

