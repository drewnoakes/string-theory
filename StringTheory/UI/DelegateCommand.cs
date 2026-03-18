using System;
using System.Windows.Input;

namespace StringTheory.UI;

internal sealed class DelegateCommand(Action execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute();

    event EventHandler? ICommand.CanExecuteChanged {  add { } remove { } }
}

internal sealed class DelegateCommand<T>(Action<T> execute) : ICommand
{
    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute((T) parameter!);

    event EventHandler? ICommand.CanExecuteChanged { add { } remove { } }
}