using System.Windows.Input;

namespace Kpt_Viewer.Infrastructure;

public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}