using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    public class GitManagerViewModel : INotifyPropertyChanged
    {
        public string RepositoryRootPath { get; set; } = string.Empty;
        private string _branchName;
        private string _repositoryPath;
        private string _statusMessage;
        private string _diffOutput;
        public ICommand GetDiffCommand { get; }
        public ICommand CreateBranchCommand { get; }
        public ICommand CommitChangesCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        public GitManagerViewModel()
        {
            CreateBranchCommand = new RelayCommand(CreateBranch);
            CommitChangesCommand = new RelayCommand(() => CommitChanges());
            GetDiffCommand = new RelayCommand(() => GetDiff());

        }
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }
        public string BranchName { get; set; } = string.Empty;

        private void CreateBranch()
        {
            if (string.IsNullOrWhiteSpace(BranchName))
            {
                // You can surface this to the user via a dialog if needed
                Debug.WriteLine("Branch name cannot be empty.");
                return;
            }

            try
            {
                // 1. Create the branch
                RunGitCommand($"checkout -b \"{BranchName}\" origin/main");

                // 2. Pull latest changes into the branch (optional: from origin/main)
                RunGitCommand("pull origin main");

                Debug.WriteLine($"Branch '{BranchName}' created and updated.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating branch: {ex.Message}");
            }
        }

        private void RunGitCommand(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = RepositoryRootPath // set this appropriately
            };

            using var process = Process.Start(psi);
            process.WaitForExit();

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            Debug.WriteLine(output);
            if (!string.IsNullOrEmpty(error))
                Debug.WriteLine($"Git error: {error}");
        }

        private void GetDiff()
        {
            StatusMessage = "Fetching Git diff...";

            try
            {
                // Example: Simulate diff retrieval
                System.Threading.Thread.Sleep(2000);  // Simulate processing delay

                DiffOutput = "Example Git Diff:\n- Line 1 changed\n+ Line 2 added";
                StatusMessage = "✅ Git diff retrieved successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Failed to get diff: {ex.Message}";
            }
        }
        private void CommitChanges()
        {
            // Example logic for committing changes
            StatusMessage = "Committing changes to Git...";

            try
            {
                // Example: Simulate a commit process
                System.Threading.Thread.Sleep(2000);  // Simulate processing delay

                StatusMessage = "✅ Changes committed successfully!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Commit failed: {ex.Message}";
            }
        }

        public string DiffOutput
        {
            get => _diffOutput;
            set { _diffOutput = value; OnPropertyChanged(); }
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        

    }
}
