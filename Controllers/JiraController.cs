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

            TriggerMerge(mergeRequest);
        }
        private void TriggerMerge(MergeRequest mergeRequest)
        {
            _gitMerger.QueueRequest(mergeRequest);
        }
    }
}
