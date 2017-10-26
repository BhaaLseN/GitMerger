using System.Runtime.Serialization;
using System.Web.Http;
using GitMerger.IssueTracking;

namespace GitMerger.Controllers
{
    public class JiraCommentController : ApiController
    {
        private readonly IJira _jira;

        public JiraCommentController(IJira jira)
        {
            _jira = jira;
        }

        [HttpGet]
        public string Get()
        {
            return "You probably want to POST with Json:\n\nExample:\n" + JsonHelper.SerializeObject(new JiraCommentRequest { IssueKey = "JRA-1234", Comment = "Insert comment here." });
        }
        [HttpPost]
        public IHttpActionResult Post([FromBody] JiraCommentRequest request)
        {
            string issueKey = request.IssueKey;
            string comment = request.Comment;
            var issue = _jira.GetIssueDetails(issueKey);
            if (issue == null)
                return BadRequest($"No issue with key '{issueKey}' exists.");
            _jira.PostComment(issueKey, comment);
            return Ok();
        }
    }

    [DataContract]
    public class JiraCommentRequest
    {
        [DataMember(Name = "key")]
        public string IssueKey { get; set; }
        [DataMember(Name = "comment")]
        public string Comment { get; set; }
    }
}
