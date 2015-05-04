using System;

namespace GitMerger.Jira
{
    public interface IJira
    {
        IssueDetails GetIssueDetails(string issueKey);
        void PostComment(string issueKey, string comment);
        bool IsClosed(IssueDetails issueDetails);
    }
}
