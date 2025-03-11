using System;
using System.Windows.Input;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers
{
    /// <summary>
    /// A command implementation for handling UI interactions in MVVM pattern.
    /// Allows binding commands in ViewModel to UI elements.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Event triggered when the execution state changes.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The action to execute.</param>
        /// <param name="canExecute">A function that determines whether the command can execute.</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Optional command parameter.</param>
        /// <returns>True if the command can execute; otherwise, false.</returns>
        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        /// <summary>
        /// Executes the command action.
        /// </summary>
        /// <param name="parameter">Optional command parameter.</param>
        public void Execute(object parameter) => _execute();

        /// <summary>
        /// Raises the CanExecuteChanged event to indicate that the execution state has changed.
        /// </summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
