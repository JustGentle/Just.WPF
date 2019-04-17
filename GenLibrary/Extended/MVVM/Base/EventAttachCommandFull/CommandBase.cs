using System;
using System.Windows.Input;
namespace GenLibrary.MVVM.Base.EventAttachCommandFull
{
    /// <summary>
    /// WPF MVVC设计模式命令基本功能类
    /// </summary>
    public class CommandBase : ICommand
    {
        #region Private Fields
        private readonly Action<object> _command;
        private readonly Func<object, bool> _canExecute;
        #endregion
        #region Constructor
        /// <summary>
        /// 实例化一个CommandBase对象
        /// </summary>
        /// <param name="command">委托一个有object类型参数的命令执行函数</param>
        /// <param name="canExecute">委托一个有object类型参数的命令是否能被执行的函数（可选）</param>  18.    /// <exception cref="ArgumentNullException">参数command不可以为null引用</param>
        public CommandBase(Action<object> command, Func<object, bool> canExecute = null)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            _canExecute = canExecute;
            _command = command;
        }
        #endregion
        #region ICommand Members
        public void Execute(object parameter)
        {
            _command(parameter);
        }
        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
                return true;
            return _canExecute(parameter);
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        #endregion
    }
}