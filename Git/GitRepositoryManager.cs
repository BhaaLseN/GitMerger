using System;
using System.Collections.Generic;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Git
{
    class GitRepositoryManager : IGitRepositoryManager
    {
        private readonly IGitSettings _gitSettings;
        public GitRepositoryManager(IGitSettings gitSettings)
        {
            _gitSettings = gitSettings;
        }
        #region IGitRepositoryManager Members

        public IEnumerable<GitRepository> FindBranch(string branchName, bool isExactBranchName)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
