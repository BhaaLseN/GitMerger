using System;

namespace GitMerger.Jira
{
    public interface IJira
    {
        IssueDetails GetIssueDetails(string issueKey);
        bool IsClosed(IssueDetails issueDetails);
    }
}
