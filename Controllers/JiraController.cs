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
            if (ShouldTryToMerge(mergeRequest))
            {
                TriggerMerge(mergeRequest);
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
        private void TriggerMerge(MergeRequest mergeRequest)
        {
            _gitMerger.QueueRequest(mergeRequest);
        }
    }
}
