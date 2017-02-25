using System;
using GitMerger.IssueTracking;

namespace GitMerger.RepositoryHandling
{
    public class MergeRequest
    {
        private MergeRequest(string mergeUserName, string mergeUserEmail)
        {
            if (string.IsNullOrEmpty(mergeUserName))
                throw new ArgumentNullException(nameof(mergeUserName), $"{nameof(mergeUserName)} is null or empty.");
            if (string.IsNullOrEmpty(mergeUserEmail))
                throw new ArgumentNullException(nameof(mergeUserEmail), $"{nameof(mergeUserEmail)} is null or empty.");

            MergeUserName = mergeUserName;
            MergeUserEmail = mergeUserEmail;

            UpstreamBranch = "master";
        }
        public MergeRequest(string mergeUserName, string mergeUserEmail, string branchName)
            : this(mergeUserName, mergeUserEmail)
        {
            BranchName = branchName;
            BranchNameIsExact = true;
        }
        public MergeRequest(string mergeUserName, string mergeUserEmail, IssueDetails issueDetails)
            : this(mergeUserName, mergeUserEmail)
        {
            if (issueDetails == null)
                throw new ArgumentNullException(nameof(issueDetails), $"{nameof(issueDetails)} is null.");

            IssueDetails = issueDetails;
            BranchName = issueDetails.Key;
            BranchNameIsExact = false;
        }
        public string UpstreamBranch { get; set; }
        public string BranchName { get; set; }
        public bool BranchNameIsExact { get; set; }
        public string MergeUserName { get; }
        public string MergeUserEmail { get; }
        public IssueDetails IssueDetails { get; }
        public string GetMergeAuthor()
        {
            return string.Format("{0} <{1}>", MergeUserName, MergeUserEmail);
        }
    }
}
