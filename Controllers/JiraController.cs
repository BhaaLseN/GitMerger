using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitMerger.IssueTracking;
using GitMerger.RepositoryHandling;
using Microsoft.AspNetCore.Mvc;

namespace GitMerger.Controllers
{
    [Route("merger/[controller]")]
    [ApiController]
    public class JiraController : ControllerBase
    {
        private readonly IGitMerger _gitMerger;

        public JiraController(IGitMerger gitMerger)
        {
            _gitMerger = gitMerger;
        }

        [HttpGet]
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
        public async Task Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                Post(await reader.ReadToEndAsync());
        }
        private void Post(string json)
        {
            var issueDetails = IssueDetails.ParseFromJson(json);
            if (issueDetails == null)
                return;
            string transitionUserName = issueDetails.TransitionUserName;
            string transitionUserMail = issueDetails.TransitionUserEMail;
            var mergeRequest = new MergeRequest(transitionUserName, transitionUserMail, issueDetails);
            _gitMerger.QueueRequest(mergeRequest);
        }
    }
}
