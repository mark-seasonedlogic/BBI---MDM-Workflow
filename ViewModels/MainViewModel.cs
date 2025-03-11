using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window. Manages UI-bound properties and commands.
    /// Implements INotifyPropertyChanged to support data binding.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly GitRepositoryManager _gitManager;
        private string _branchName;
        private string _statusMessage;

        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            _gitManager = new GitRepositoryManager(System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "IntuneConfigRepo"));

            CreateBranchCommand = new RelayCommand(CreateBranch);
        }

        /// <summary>
        /// Gets or sets the branch name input from the user.
        /// </summary>
        public string BranchName
        {
            get => _branchName;
            set
            {
                _branchName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the status message to provide feedback to the user.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Command for creating a new Git branch.
        /// </summary>
        public ICommand CreateBranchCommand { get; }

        /// <summary>
        /// Creates a new Git branch using the entered branch name.
        /// Updates the status message to provide feedback to the user.
        /// </summary>
        private void CreateBranch()
        {
            if (!string.IsNullOrWhiteSpace(BranchName))
            {
                _gitManager.CreateBranch(BranchName);
                StatusMessage = $"Branch '{BranchName}' created successfully!";
            }
            else
            {
                StatusMessage = "Branch name cannot be empty!";
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event to notify UI of property updates.
        /// </summary>
        /// <param name="name">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}