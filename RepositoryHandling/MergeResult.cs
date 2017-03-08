using System;

namespace GitMerger.RepositoryHandling
{
    public class MergeResult
    {
        public MergeResult(GitResult result, GitRepositoryBranch branch)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result), $"{nameof(result)} is null.");
            if (branch == null)
                throw new ArgumentNullException(nameof(branch), $"{nameof(branch)} is null.");

            Result = result;
            Branch = branch;
        }

        public GitResultType ResultType
        {
            get { return Result.ResultType; }
        }

        public GitResult Result { get; }
        public GitRepositoryBranch Branch { get; }
    }
}
