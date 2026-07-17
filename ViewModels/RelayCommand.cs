using System;
using System.Windows.Input;

namespace MeetingBar.Windows.ViewModels
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        public RelayCommand(Action execute, Func<bool> can = null) : this(_ => execute(), can == null ? null : new Func<object, bool>(_ => can())) { }
        public RelayCommand(Action<object> execute, Func<object, bool> can = null) { _execute = execute; _canExecute = can; }
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
