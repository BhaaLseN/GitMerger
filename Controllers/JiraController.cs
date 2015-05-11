using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
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
        public async void Post(HttpRequestMessage request)
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
            // should the current transition not be one of the expected ones indicating "we went to Closed", skip the trigger aswell.
            if (!_jiraSettings.ValidTransitions.Contains(mergeRequest.IssueDetails.TransitionId))
                return false;
            // do we like its current resolution? trigger a merge.
            if (_jiraSettings.ValidResolutions.Contains(mergeRequest.IssueDetails.Resolution))
                return true;
            return false;
        }
        private void TriggerMerge(MergeRequest mergeRequest)
        {
            _gitMerger.QueueRequest(mergeRequest);
        }
    }
}
