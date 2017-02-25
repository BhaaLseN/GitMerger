using System;
using GitMerger.IssueTracking;

namespace GitMerger.RepositoryHandling
{
    public class MergeRequest
    {
        private readonly string _mergeUserName;
        private readonly string _mergeUserEmail;
        private readonly IssueDetails _issueDetails;

        private MergeRequest(string mergeUserName, string mergeUserEmail)
        {
            if (string.IsNullOrEmpty(mergeUserName))
                throw new ArgumentNullException("mergeUserName", "mergeUserName is null or empty.");
            if (string.IsNullOrEmpty(mergeUserEmail))
                throw new ArgumentNullException("mergeUserEmail", "mergeUserEmail is null or empty.");

            _mergeUserName = mergeUserName;
            _mergeUserEmail = mergeUserEmail;

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
                throw new ArgumentNullException("issueDetails", "issueDetails is null.");

            _issueDetails = issueDetails;
            BranchName = issueDetails.Key;
            BranchNameIsExact = false;
        }
        public string UpstreamBranch { get; set; }
        public string BranchName { get; set; }
        public bool BranchNameIsExact { get; set; }
        public string MergeUserName
        {
            get { return _mergeUserName; }
        }
        public string MergeUserEmail
        {
            get { return _mergeUserEmail; }
        }
        public IssueDetails IssueDetails
        {
            get { return _issueDetails; }
        }
        public string GetMergeAuthor()
        {
            return string.Format("{0} <{1}>", MergeUserName, MergeUserEmail);
        }
    }
}
