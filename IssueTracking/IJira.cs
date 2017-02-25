namespace GitMerger.IssueTracking
{
    public interface IJira
    {
        IssueDetails GetIssueDetails(string issueKey);
        void PostComment(string issueKey, string comment);
    }
}
