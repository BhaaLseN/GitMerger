using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMerger.Infrastructure.Settings;
using GitMerger.Jira;

namespace GitMerger.Git
{
    class GitMerger : IGitMerger
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<GitMerger>();

        private readonly BlockingCollection<MergeRequest> _mergeRequests = new BlockingCollection<MergeRequest>();
        private readonly IJira _jira;
        private readonly IGitSettings _gitSettings;
        private readonly IGitRepositoryManager _repositoryManager;

        public GitMerger(IGitRepositoryManager repositoryManager, IGitSettings gitSettings, IJira jira)
        {
            _repositoryManager = repositoryManager;
            _gitSettings = gitSettings;
            _jira = jira;
            Task.Run(() => HandleMergeRequests());
        }
        #region IGitMerger Members

        public void QueueRequest(MergeRequest mergeRequest)
        {
            Task.Run(() =>
            {
                bool shouldMerge;
                if (mergeRequest.IssueDetails == null)
                {
                    Logger.Info(m => m("Got a merge request without associated Jira issue. Guess we should merge that one either way."));
                    shouldMerge = true;
                }
                else
                {
                    Logger.Debug(m => m("Waiting a bit before queuing merge request for '{0}'", mergeRequest.IssueDetails.Key));
                    // wait a bit before actually queuing the request; someone might have accidentally closed the Jira issue
                    Thread.Sleep(_gitSettings.MergeDelay);

                    var issueDetails = _jira.GetIssueDetails(mergeRequest.IssueDetails.Key);
                    if (issueDetails == null)
                    {
                        Logger.Warn(m => m("Jira didn't return any issue information while trying to check if we should still merge '{0}'; not doing a merge.", mergeRequest.IssueDetails.Key));
                        shouldMerge = false;
                    }
                    else
                    {
                        shouldMerge = _jira.IsClosed(issueDetails);
                        Logger.Info(m => m("Related Jira issue is {0}closed, {0}preceding with merge.", shouldMerge ? "" : "not "));
                    }
                }

                if (shouldMerge)
                {
                    // HandleMergeRequests should only get valid ones, so check if the request is still valid
                    _mergeRequests.Add(mergeRequest);
                }
            });
        }

        #endregion
        private void HandleMergeRequests()
        {
            while (!_mergeRequests.IsCompleted)
            {
                var mergeRequest = _mergeRequests.Take();
                if (Merge(mergeRequest))
                {
                    Console.WriteLine("Merged.");
                    if (mergeRequest.IssueDetails != null)
                        _jira.PostComment(mergeRequest.IssueDetails.Key, string.Format("Successfully merged '{0}' into '{1}' (on behalf of {2}).",
                            mergeRequest.BranchName, mergeRequest.UpstreamBranch, MakeJiraReference(mergeRequest.IssueDetails.TransitionUserKey)));
                }
                else
                {
                    Console.WriteLine("Could not merge...");
                    if (mergeRequest.IssueDetails != null)
                        _jira.PostComment(mergeRequest.IssueDetails.Key, string.Format("Failed to automatically merge '{0}' into '{1}'.\r\n\r\n{2} will need to do this by hand.",
                            mergeRequest.BranchName, mergeRequest.UpstreamBranch, MakeJiraReference(mergeRequest.IssueDetails.AssigneeUserKey)));
                }
            }
        }

        private static string MakeJiraReference(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return "Someone";
            return string.Format("[~{0}]", userName);
        }
        private bool Merge(MergeRequest mergeRequest)
        {
            var branches = _repositoryManager.FindBranch(mergeRequest.BranchName, mergeRequest.BranchNameIsExact).ToArray();
            if (!branches.Any())
                return false;

            int successfulMerges = 0;
            foreach (var branch in branches)
            {
                if (_repositoryManager.MergeAndPush(branch.Repository, branch.BranchName, mergeRequest.UpstreamBranch, mergeRequest.GetMergeAuthor()))
                    successfulMerges++;
                // TODO: remember/report on successful/failed merges
            }
            return successfulMerges == branches.Count();
        }
    }
}
