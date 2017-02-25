using System;

namespace GitMerger.RepositoryHandling
{
    public class GitRepositoryBranch
    {
        public GitRepositoryBranch(GitRepository repository, string branchName)
        {
            if (repository == null)
                throw new ArgumentNullException(nameof(repository), $"{nameof(repository)} is null.");
            if (string.IsNullOrEmpty(branchName))
                throw new ArgumentNullException(nameof(branchName), $"{nameof(branchName)} is null or empty.");
            Repository = repository;
            BranchName = branchName;
        }
        public GitRepository Repository { get; private set; }
        public string BranchName { get; private set; }
    }
}
