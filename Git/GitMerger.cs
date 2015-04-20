using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace GitMerger.Git
{
    class GitMerger : IGitMerger
    {
        private readonly BlockingCollection<MergeRequest> _mergeRequests = new BlockingCollection<MergeRequest>();
        private readonly IGitRepositoryManager _repositoryManager;
        public GitMerger(IGitRepositoryManager repositoryManager)
        {
            _repositoryManager = repositoryManager;
            Task.Run(() => HandleMergeRequests());
        }
        #region IGitMerger Members

        public void QueueRequest(MergeRequest mergeRequest)
        {
            _mergeRequests.Add(mergeRequest);
        }

        #endregion
        private void HandleMergeRequests()
        {
            while (!_mergeRequests.IsCompleted)
            {
                var mergeRequest = _mergeRequests.Take();
                if (Merge(mergeRequest))
                    Console.WriteLine("Merged.");
                else
                    Console.WriteLine("Could not merge...");
            }
        }
        private bool Merge(MergeRequest mergeRequest)
        {
            var branches = _repositoryManager.FindBranch(mergeRequest.BranchName, mergeRequest.BranchNameIsExact).ToArray();
            if (!branches.Any())
                return false;

            int successfulMerges = 0;
            foreach (var branch in branches)
            {
                if (_repositoryManager.MergeAndPush(branch.Repository, branch.BranchName, "master"))
                    successfulMerges++;
                // TODO: remember/report on successful/failed merges
            }
            return successfulMerges == branches.Count();
        }
    }
}
