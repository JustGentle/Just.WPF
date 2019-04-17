using System;
using System.Windows.Input;
using System.Diagnostics;
namespace GenLibrary.MVVM.Base
{
    public class RelayCommand : ICommand
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="execute">触发这个command之后要执行的方法</param>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }
        public event EventHandler CanExecuteChanged
        { 
            add 
            { 
             if (_canExecute != null) 
             CommandManager.RequerySuggested += value; 
            } 
            remove 
            { 
             if (_canExecute != null) 
             CommandManager.RequerySuggested -= value; 
            } 
        }
        /// <summary>
        /// 执行活动
        /// </summary>
        /// <param name="parameter">传入的参数</param>
        public void Execute(object parameter)
        {
            _execute();
        }
        readonly Action _execute;
        readonly Func<bool> _canExecute;
    }
    public class RelayCommand<T> : ICommand where T : class
    {
        private readonly Action<T> _execute;

        private readonly Func<bool> _canExecute;

        public RelayCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<T> execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute(parameter as T);
        }
    }

}//结束命名空间