using System.Runtime.Serialization;
using GitMerger.IssueTracking;
using Microsoft.AspNetCore.Mvc;

namespace GitMerger.Controllers
{
    [Route("merger/[controller]")]
    [ApiController]
    public class JiraCommentController : ControllerBase
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
        public ActionResult Post([FromBody] JiraCommentRequest request)
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
