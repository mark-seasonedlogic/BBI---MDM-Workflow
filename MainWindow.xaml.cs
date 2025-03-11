using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using LibGit2Sharp;
using System.IO;
using NLog;

namespace BBIHardwareSupport.MDM.IntuneConfigManager
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private string repoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IntuneConfigRepo");

        public MainWindow()
        {
            this.InitializeComponent();
            InitializeRepo();
        }

        private void InitializeRepo()
        {
            if (!Directory.Exists(repoPath))
            {
                Directory.CreateDirectory(repoPath);
                Repository.Init(repoPath);
                logger.Info("Initialized new Git repository.");
            }
            else if (!Repository.IsValid(repoPath))
            {
                Repository.Init(repoPath);
                logger.Info("Reinitialized Git repository.");
            }
            else
            {
                logger.Info("Git repository already exists.");
            }
        }

        private void CreateBranch(string branchName)
        {
            using (var repo = new Repository(repoPath))
            {
                repo.CreateBranch(branchName);
                logger.Info($"Created new branch: {branchName}");
            }
        }
        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            logger.Info("Button Clicked");
        }
    }
}
