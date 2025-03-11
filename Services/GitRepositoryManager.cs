using LibGit2Sharp;
using System;
using System.IO;
using NLog;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    /// <summary>
    /// Manages Git repository operations such as initialization and branching.
    /// Encapsulates Git functionality using LibGit2Sharp.
    /// </summary>
    public class GitRepositoryManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string repoPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="GitRepositoryManager"/> class.
        /// </summary>
        /// <param name="repositoryPath">The file system path to the Git repository.</param>
        public GitRepositoryManager(string repositoryPath)
        {
            repoPath = repositoryPath ?? throw new ArgumentNullException(nameof(repositoryPath));
            InitializeRepo();
        }

        /// <summary>
        /// Initializes the Git repository if it does not already exist.
        /// </summary>
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

        /// <summary>
        /// Creates a new Git branch in the repository.
        /// </summary>
        /// <param name="branchName">The name of the new branch.</param>
        public void CreateBranch(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                throw new ArgumentException("Branch name cannot be null or empty.", nameof(branchName));

            using (var repo = new Repository(repoPath))
            {
                repo.CreateBranch(branchName);
                logger.Info($"Created new branch: {branchName}");
            }
        }
    }
}