using System;

namespace GitMerger.Git
{
    public class MergeResult
    {
        public MergeResult(GitResult result, GitRepositoryBranch branch)
        {
            if (result == null)
                throw new ArgumentNullException("result", "result is null.");
            if (branch == null)
                throw new ArgumentNullException("branch", "branch is null.");

            Result = result;
            Branch = branch;
        }
        public bool Success
        {
            get { return Result.Success; }
        }
        public GitResult Result { get; private set; }
        public GitRepositoryBranch Branch { get; private set; }
    }
}
