using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitMerger.Infrastructure.Settings;
using GitMerger.Jira;

namespace GitMerger.RepositoryHandling
{
    class GitMerger : IGitMerger
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<GitMerger>();

        private readonly BlockingCollection<MergeRequest> _mergeRequests = new BlockingCollection<MergeRequest>();
        private readonly IJiraSettings _jiraSettings;
        private readonly IJira _jira;
        private readonly IGitSettings _gitSettings;
        private readonly IGitRepositoryManager _repositoryManager;

        public GitMerger(IGitRepositoryManager repositoryManager, IGitSettings gitSettings, IJira jira, IJiraSettings jiraSettings)
        {
            _repositoryManager = repositoryManager;
            _gitSettings = gitSettings;
            _jira = jira;
            _jiraSettings = jiraSettings;
            Task.Run(() => HandleMergeRequests());
        }
        #region IGitMerger Members

        public void QueueRequest(MergeRequest mergeRequest)
        {
            // initially update the merge request with information from the POST request (or wherever it came from).
            // we might do another update later to make sure the data is still relevant.
            UpdateIssueDetails(mergeRequest, mergeRequest.IssueDetails);
            if (!ShouldTryToMerge(mergeRequest))
                return;

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
                        shouldMerge = !ShouldPreventAutomerge(issueDetails);
                        Logger.Info(m => m("Related Jira issue indicates it should {0}be merged, {0}preceding with merge.", shouldMerge ? "" : "not "));
                        if (shouldMerge)
                            UpdateIssueDetails(mergeRequest, issueDetails);
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

        private void UpdateIssueDetails(MergeRequest mergeRequest, IssueDetails issueDetails)
        {
            if (mergeRequest == null || issueDetails == null)
                return;
            if (issueDetails.CustomFields != null)
            {
                // see if we have an upstream branch name set; so we can override the initial guess of "master"
                if (!string.IsNullOrEmpty(_jiraSettings.UpstreamBranchFieldName) &&
                    issueDetails.CustomFields.Contains(_jiraSettings.UpstreamBranchFieldName))
                {
                    Logger.Debug(m => m("Checking {0} potential upstream branches: {1}",
                        issueDetails.CustomFields[_jiraSettings.UpstreamBranchFieldName].Count(),
                        string.Join(", ", issueDetails.CustomFields[_jiraSettings.UpstreamBranchFieldName])));
                    // technically, this could be a list or something. we'll just ignore all but the first one and hope it works out.
                    string upstreamBranch = issueDetails.CustomFields[_jiraSettings.UpstreamBranchFieldName].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(upstreamBranch))
                    {
                        Logger.Info(m => m("Merge request for '{0}' uses upstream branch '{1}' instead of default '{2}'.", mergeRequest.BranchName, upstreamBranch, mergeRequest.UpstreamBranch));
                        mergeRequest.UpstreamBranch = upstreamBranch;
                    }
                    else
                    {
                        Logger.Info(m => m("Merge request for '{0}' uses default upstream branch '{1}'.", mergeRequest.BranchName, mergeRequest.UpstreamBranch));
                    }
                }

                // see if we have an actual branch name set instead of using the issue key
                if (!string.IsNullOrEmpty(_jiraSettings.BranchFieldName) &&
                    issueDetails.CustomFields.Contains(_jiraSettings.BranchFieldName))
                {
                    Logger.Debug(m => m("Checking {0} potential branch names: {1}",
                        issueDetails.CustomFields[_jiraSettings.BranchFieldName].Count(),
                        string.Join(", ", issueDetails.CustomFields[_jiraSettings.BranchFieldName])));
                    // technically, this could be a list or something. we'll just ignore all but the first one and hope it works out.
                    string branchName = issueDetails.CustomFields[_jiraSettings.BranchFieldName].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(branchName))
                    {
                        Logger.Info(m => m("Merge request for '{0}' uses branch name '{1}' instead of issue key.", mergeRequest.BranchName, branchName));
                        mergeRequest.BranchName = branchName;
                        mergeRequest.BranchNameIsExact = true;
                    }
                    else
                    {
                        Logger.Info(m => m("Merge request for '{0}' uses issue key as branch name.", mergeRequest.BranchName));
                    }
                }
            }
        }

        private bool ShouldTryToMerge(MergeRequest mergeRequest)
        {
            // no issue? try to merge anyways.
            if (mergeRequest.IssueDetails == null)
                return true;
            // Only trigger merges for valid resolution states that indicate a successful closing of the issue
            // not a transition? most likely not a valid trigger for us.
            if (!mergeRequest.IssueDetails.IsTransition)
                return false;
            // never merge if the assignee opted out of the automatic merge
            if (ShouldPreventAutomerge(mergeRequest.IssueDetails))
                return false;
            // should the current transition not be one of the expected ones indicating "we went to Closed", skip the trigger aswell.
            if (!_jiraSettings.ValidTransitions.Contains(mergeRequest.IssueDetails.TransitionId))
                return false;
            // do we like its current resolution? trigger a merge.
            if (_jiraSettings.ValidResolutions.Contains(mergeRequest.IssueDetails.Resolution))
                return true;
            return false;
        }
        private bool ShouldPreventAutomerge(IssueDetails issueDetails)
        {
            // no issue, no opt-out
            if (issueDetails == null)
                return false;
            // no configuration for this? no opt-out
            if (string.IsNullOrEmpty(_jiraSettings.DisableAutomergeFieldName))
                return false;
            if (string.IsNullOrEmpty(_jiraSettings.DisableAutomergeFieldValue))
                return false;
            // no matching custom field? no opt-out
            if (issueDetails.CustomFields == null)
                return false;
            if (!issueDetails.CustomFields.Contains(_jiraSettings.DisableAutomergeFieldName))
                return false;

            // apparently the custom field is set; lets see if it has the value that indicates opt-out for the automerge
            return issueDetails.CustomFields[_jiraSettings.DisableAutomergeFieldName].Contains(_jiraSettings.DisableAutomergeFieldValue);
        }

        private void HandleMergeRequests()
        {
            while (!_mergeRequests.IsCompleted)
            {
                var mergeRequest = _mergeRequests.Take();
                var mergeResults = Merge(mergeRequest);
                if (!mergeResults.Any())
                {
                    Logger.Info(m => m("No merge results returned from an attempted merge. This usually means that none of the configured repositories had a matching branch"));
                    // TODO: should we treat this as a case where we want to ping the Jira issue?
                }
                else
                {
                    string jiraComment = BuildJiraComment(mergeRequest, mergeResults);
                    _jira.PostComment(mergeRequest.IssueDetails.Key, jiraComment);
                }
            }
        }

        // TODO: it might make sense to move this to a separate class (that can be replaced with a more generic one that supports templates, for example)
        private static string BuildJiraComment(MergeRequest mergeRequest, IEnumerable<MergeResult> mergeResults)
        {
            var sb = new StringBuilder();

            int totalAttempts = mergeResults.Count();
            int failedAttempts = mergeResults.Count(r => !r.Success);
            bool multipleRepositories = totalAttempts > 1;

            // show a small status indicator if everything worked, something went wrong or everything failed
            if (failedAttempts == 0)
                sb.Append("(/) ");
            else if (totalAttempts == failedAttempts)
                sb.Append("(x) ");
            else
                sb.Append("(!) ");

            // notify the user that triggered the merge by mentioning them. if this was a mistake, they should get the notice that it's their fault :)
            sb.AppendFormat("On behalf of {0}, ", MentionJiraUser(mergeRequest.IssueDetails.TransitionUserKey));
            if (multipleRepositories)
            {
                // for multiple repositories, include the merge count as summary
                if (failedAttempts == 0)
                    sb.AppendFormat("we successfully merged branches in {0} repositories for you.", totalAttempts);
                else
                    sb.AppendFormat("we tried to merge branches in {0} repositories for you, but {1} of them failed.", totalAttempts, failedAttempts);
            }
            else
            {
                // for a single repository, just include the result
                if (failedAttempts == 0)
                    sb.Append("we successfully merged the branch for you.");
                else
                    sb.Append("we tried to merge the branch for you, but failed.");
            }

            // notify the assignee that they might have some extra work to do (but only if something went wrong)
            if (failedAttempts > 0)
                sb.AppendLine()
                    .AppendFormat("Some steps might need to be performed by hand (by {0} for example).", MentionJiraUser(mergeRequest.IssueDetails.AssigneeUserKey));

            // empty line between the summary and the repository notes
            sb.AppendLine().AppendLine();

            foreach (var mergeResult in mergeResults)
            {
                // separate summary and info with a vertical line; and repository infos in case of multiple ones
                sb.AppendLine("----");

                if (multipleRepositories)
                {
                    // in case of multiple repositories, prefix them with the repository identifier (usually the url)
                    sb.AppendFormat("*Repository {0}*: ", EscapeJiraString(mergeResult.Branch.Repository.RepositoryIdentifier));
                }

                if (mergeResult.Success)
                {
                    // simple success message with status indicator icon
                    if (!string.IsNullOrEmpty(mergeResult.Result.Message))
                        sb.AppendFormat("(/) {0}", mergeResult.Result.Message);
                    else
                        sb.AppendFormat("(/) Branch {0} successfully merged into {1}.", mergeResult.Branch.BranchName, mergeRequest.UpstreamBranch);
                    // TODO: maybe include revert instructions?
                }
                else
                {
                    // failure message with status indicator icon...
                    sb.AppendFormat("(x) Automatic merge failed: {0}", mergeResult.Result.Message).AppendLine();
                    // ...followed by a brief hint on what might be necessary to merge this branch by hand...
                    sb.AppendLine("This usually means that you need to merge this branch by hand using the following commands (for example, command line only):");
                    sb.Append("{noformat:title=Merge Instructions (command line)}");
                    sb.AppendFormat("git checkout {0}", mergeRequest.UpstreamBranch).AppendLine();
                    sb.AppendFormat("git merge --no-ff {0}", mergeResult.Branch.BranchName).AppendLine();
                    sb.Append("- resolve merge conflicts here, if necessary -").AppendLine();
                    sb.AppendFormat("git push origin {0} :{1}", mergeRequest.UpstreamBranch, mergeResult.Branch.BranchName).AppendLine();
                    sb.Append("{noformat}");

                    // ...followed by command line/stdout/stderr
                    string commandLine = string.Format("\"{0}\" {1}",
                        mergeResult.Result.ExecuteResult.StartInfo.FileName.Trim('"', '\''),
                        mergeResult.Result.ExecuteResult.StartInfo.Arguments ?? string.Empty);
                    string stdout = string.Join("\r\n", mergeResult.Result.ExecuteResult.StdoutLines);
                    string stderr = string.Join("\r\n", mergeResult.Result.ExecuteResult.StderrLines);

                    if (!string.IsNullOrWhiteSpace(stdout) || !string.IsNullOrWhiteSpace(stderr) || !string.IsNullOrWhiteSpace(commandLine.Trim(' ', '"')))
                        sb.AppendLine()
                            .Append("Further information and process outputs that may help you:");

                    if (!string.IsNullOrWhiteSpace(commandLine.Trim(' ', '"')))
                        sb.AppendLine()
                            .Append("{noformat:title=Command Line}")
                            .Append(commandLine)
                            .Append("{noformat}");
                    if (!string.IsNullOrWhiteSpace(stdout))
                        sb.AppendLine()
                            .Append("{noformat:title=Standard Output}")
                            .Append(stdout)
                            .Append("{noformat}");
                    if (!string.IsNullOrWhiteSpace(stderr))
                        sb.AppendLine()
                            .Append("{noformat:title=Standard Error}")
                            .Append(stderr)
                            .Append("{noformat}");
                }

                // empty line between repository notes and the end
                sb.AppendLine().AppendLine();
            }

            // append a short note that this is an automated comment. subscript/italic to make it less intrusive
            sb.Append("~_Automated comment by [GitMerger|https://github.com/BhaaLseN/GitMerger]_~");

            return sb.ToString();
        }

        /// <summary>
        /// Returns a Jira mention for the given <paramref name="userName"/>.
        /// </summary>
        /// <param name="userName">Any valid Jira user name.</param>
        /// <returns><c>[~user.name]</c> or <c>"Someone"</c> when <paramref name="userName"/> is empty.</returns>
        private static string MentionJiraUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return "Someone";
            return string.Format("[~{0}]", userName);
        }
        private static readonly string[] JiraSpecialCharacters =
        {
            "@", // @mention syntax
            "{", "}", // markdown braces
            "[", "]", // links
            "*", "_", // bold, underline
            "-", "+", // inserted, deleted
            "~", "^", // subscript, superscript
            "#", // numbered list
            "!", // attachment/image inline marker
            "|", // table markup
        };
        /// <summary>
        /// Escapes a string that might contain special characters that hold meaning in Jira markdown syntax
        /// </summary>
        /// <param name="jiraString">Any string to escape</param>
        /// <returns>An escaped version of <paramref name="jiraString"/>, or <see cref="string.Empty"/> if <paramref name="jiraString"/> is empty.</returns>
        private static string EscapeJiraString(string jiraString)
        {
            if (string.IsNullOrEmpty(jiraString))
                return string.Empty;

            // backslash is a replace char and two backslashes aren't going to cut it (https://jira.atlassian.com/browse/JRA-9258)
            jiraString = jiraString.Replace("\\", "&#92;");
            foreach (string escapeText in JiraSpecialCharacters)
                jiraString = jiraString.Replace(escapeText, "\\" + escapeText);
            return jiraString;
        }
        private IEnumerable<MergeResult> Merge(MergeRequest mergeRequest)
        {
            Logger.Info(m => m("Handling merge request from '{0}' to merge '{1}' into '{2}'.", mergeRequest.GetMergeAuthor(), mergeRequest.BranchName, mergeRequest.UpstreamBranch));
            var results = new List<MergeResult>();
            var branches = _repositoryManager.FindBranch(mergeRequest.BranchName, mergeRequest.BranchNameIsExact).ToArray();
            if (!branches.Any())
                return results;

            foreach (var branch in branches)
            {
                var result = _repositoryManager.MergeAndPush(branch.Repository, branch.BranchName, mergeRequest.UpstreamBranch, mergeRequest.GetMergeAuthor());
                results.Add(new MergeResult(result, branch));
            }
            return results;
        }
    }
}
