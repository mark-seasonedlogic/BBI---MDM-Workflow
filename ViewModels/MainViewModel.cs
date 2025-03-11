using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
using NLog;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    /// <summary>
    /// ViewModel for the main application window. Manages UI-bound properties and commands.
    /// Implements INotifyPropertyChanged to support data binding.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private GitRepositoryManager _gitManager;
        private string _repositoryPath;
        private string _branchName;
        private string _statusMessage;
        private string _diffOutput;

        /// <summary>
        /// Event triggered when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            RepositoryPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "IntuneConfigRepo");

            _gitManager = new GitRepositoryManager(RepositoryPath);

            CreateBranchCommand = new RelayCommand(CreateBranch);
            CommitChangesCommand = new RelayCommand(() => CommitChanges("Default commit message"));
            GetDiffCommand = new RelayCommand(GetDiff);
            CreateTagCommand = new RelayCommand(() => CreateTag("v1.0.0", "Initial tag"));
            GetCommitHistoryCommand = new RelayCommand(GetCommitHistory);
            SelectRepositoryCommand = new RelayCommand(SelectRepository);
        }

        /// <summary>
        /// Gets or sets the repository path.
        /// </summary>
        public string RepositoryPath
        {
            get => _repositoryPath;
            set
            {
                _repositoryPath = value;
                OnPropertyChanged();
            }
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
        /// Gets or sets the diff output for display.
        /// </summary>
        public string DiffOutput
        {
            get => _diffOutput;
            set
            {
                _diffOutput = value;
                OnPropertyChanged();
            }
        }

        public ICommand CreateBranchCommand { get; }
        public ICommand CommitChangesCommand { get; }
        public ICommand GetDiffCommand { get; }
        public ICommand CreateTagCommand { get; }
        public ICommand GetCommitHistoryCommand { get; }
        public ICommand SelectRepositoryCommand { get; }

        private void CreateBranch()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(BranchName))
                {
                    _gitManager.CreateBranch(BranchName);
                    StatusMessage = $"Branch '{BranchName}' created successfully!";
                    logger.Info(StatusMessage);
                }
                else
                {
                    StatusMessage = "Branch name cannot be empty!";
                    logger.Warn(StatusMessage);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error creating branch");
            }
        }

        private void CommitChanges(string message)
        {
            try
            {
                _gitManager.CommitChanges(message);
                StatusMessage = "Changes committed successfully.";
                logger.Info(StatusMessage);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error committing changes");
            }
        }

        private void GetDiff()
        {
            try
            {
                DiffOutput = _gitManager.GetDiff();
                logger.Info("Git diff retrieved successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error retrieving Git diff");
            }
        }

        private void CreateTag(string tagName, string message)
        {
            try
            {
                _gitManager.CreateTag(tagName, message);
                StatusMessage = $"Tag '{tagName}' created successfully.";
                logger.Info(StatusMessage);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error creating tag");
            }
        }

        private void GetCommitHistory()
        {
            try
            {
                var history = _gitManager.GetCommitHistory();
                StatusMessage = string.Join("\n", history);
                logger.Info("Commit history retrieved successfully.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error retrieving commit history");
            }
        }

        private async void SelectRepository()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            // Retrieve the window handle and associate it with the picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                RepositoryPath = folder.Path;
                _gitManager = new GitRepositoryManager(RepositoryPath);
                StatusMessage = $"Repository set to: {RepositoryPath}";
                logger.Info(StatusMessage);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
