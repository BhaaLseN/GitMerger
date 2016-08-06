using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using GitMerger.Git;
using GitMerger.Infrastructure.Settings;
using GitMerger.Jira;

namespace GitMerger.Controllers
{
    public class JiraController : ApiController
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<JiraController>();

        private readonly IGitMerger _gitMerger;
        private readonly IJiraSettings _jiraSettings;

        public JiraController(IGitMerger gitMerger, IJiraSettings jiraSettings)
        {
            _gitMerger = gitMerger;
            _jiraSettings = jiraSettings;
        }

        public string Get()
        {
            return "nuuuuh";
        }
        [HttpPost]
        // must return a Task rather than void, or request.Content.ReadAsStringAsync will throw an ObjectDisposedException
        // after the first request was successful. Apparently ASP.Net (and also WebApi) do not like "fire and forget" methods.
        // Eric Lippert explains this in http://stackoverflow.com/a/8043882 (basically: nothing can "await" an "async void"
        // method, but that in turn doesn't allow the framework to know when to properly free/dispose/recycle/whatever the stuff
        // passed in. in short: use "async Task" even if you won't return anything :S)
        public async Task Post(HttpRequestMessage request)
        {
            Post(await request.Content.ReadAsStringAsync());
        }
        private void Post(string json)
        {
            var issueDetails = IssueDetails.ParseFromJson(json);
            if (issueDetails == null)
                return;
            string transitionUserName = issueDetails.TransitionUserName;
            string transitionUserMail = issueDetails.TransitionUserEMail;
            var mergeRequest = new MergeRequest(transitionUserName, transitionUserMail, issueDetails);

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

            TriggerMerge(mergeRequest);
        }
        private void TriggerMerge(MergeRequest mergeRequest)
        {
            _gitMerger.QueueRequest(mergeRequest);
        }
    }
}
