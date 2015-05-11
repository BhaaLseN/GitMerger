using System.Collections.Generic;

namespace GitMerger.Git
{
    public interface IGitRepositoryManager
    {
        IEnumerable<GitRepositoryBranch> FindBranch(string branchName, bool isExactBranchName);
        GitResult MergeAndPush(GitRepository repository, string branchName, string mergeInto, string mergeAuthor);
    }
}
