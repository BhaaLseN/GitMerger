using System.Collections.Generic;
using System;
namespace GitMerger.Git
{
    public interface IGitRepositoryManager
    {
        IEnumerable<GitRepository> FindBranch(string branchName, bool isExactBranchName);
    }
}
