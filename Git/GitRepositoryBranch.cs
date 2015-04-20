using System;

namespace GitMerger.Git
{
    public class GitRepositoryBranch
    {
        public GitRepositoryBranch(GitRepository repository, string branchName)
        {
            if (repository == null)
                throw new ArgumentNullException("repository", "repository is null.");
            if (string.IsNullOrEmpty(branchName))
                throw new ArgumentNullException("branchName", "branchName is null or empty.");
            Repository = repository;
            BranchName = branchName;
        }
        public GitRepository Repository { get; private set; }
        public string BranchName { get; private set; }
    }
}
