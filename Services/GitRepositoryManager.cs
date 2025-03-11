using LibGit2Sharp;
using System;
using System.IO;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    /// <summary>
    /// Manages Git repository operations such as initialization, committing, branching, and tagging.
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

        /// <summary>
        /// Commits changes to the repository.
        /// </summary>
        /// <param name="message">The commit message.</param>
        public void CommitChanges(string message)
        {
            using (var repo = new Repository(repoPath))
            {
                Commands.Stage(repo, "*");
                var author = new Signature("User", "user@example.com", DateTime.Now);
                repo.Commit(message, author, author);
                logger.Info($"Committed changes: {message}");
            }
        }

        /// <summary>
        /// Gets the diff between the latest commit and the working directory.
        /// </summary>
        /// <returns>The diff as a string.</returns>
        public string GetDiff()
        {
            using (var repo = new Repository(repoPath))
            {
                var changes = repo.Diff.Compare<Patch>(repo.Head.Tip.Tree, DiffTargets.WorkingDirectory);
                return changes.Content;
            }
        }

        /// <summary>
        /// Tags the latest commit with the specified tag name.
        /// </summary>
        /// <param name="tagName">The tag name.</param>
        /// <param name="message">The tag message.</param>
        public void CreateTag(string tagName, string message)
        {
            using (var repo = new Repository(repoPath))
            {
                repo.Tags.Add(tagName, repo.Head.Tip, new Signature("User", "user@example.com", DateTime.Now), message);
                logger.Info($"Created tag {tagName}: {message}");
            }
        }

        /// <summary>
        /// Retrieves the commit history of the repository.
        /// </summary>
        /// <returns>A list of commit messages.</returns>
        public List<string> GetCommitHistory()
        {
            using (var repo = new Repository(repoPath))
            {
                return repo.Commits.Take(10).Select(c => $"{c.Id}: {c.MessageShort}").ToList();
            }
        }
    }
}
