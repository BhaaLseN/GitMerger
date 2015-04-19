using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
namespace GitMerger.Git
{
    class GitMerger : IGitMerger
    {
        private readonly BlockingCollection<MergeRequest> _mergeRequests = new BlockingCollection<MergeRequest>();
        public GitMerger()
        {
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
            return false;
        }
    }
}
