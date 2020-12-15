using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CodeIndex.VisualStudioExtension
{
    public class CommonCommand : ICommand
    {
        readonly Action<object> execute;
        readonly Predicate<object> canExecute;

        public CommonCommand(Action<object> execute) : this(execute, null)
        {
        }

        public CommonCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object parameters)
        {
            return canExecute == null || canExecute(parameters);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object parameters)
        {
            execute(parameters);
        }
    }

    public class AsyncCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        bool isExecuting;
        readonly Func<Task> execute;
        readonly Func<bool> canExecute;
        readonly Action<Exception> errorHandler;

        public AsyncCommand(
            Func<Task> execute,
            Func<bool> canExecute,
            Action<Exception> errorHandler)
        {
            this.execute = execute;
            this.canExecute = canExecute;
            this.errorHandler = errorHandler;
        }

        public bool CanExecute()
        {
            return !isExecuting && (canExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    isExecuting = true;
                    await execute();
                }
                finally
                {
                    isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return CanExecute();
        }

        public void Execute(object parameter)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (Exception ex)
                {
                    errorHandler?.Invoke(ex);
                }
            });
        }
    }
}
