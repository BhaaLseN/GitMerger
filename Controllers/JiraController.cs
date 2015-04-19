using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;
using GitMerger.Git;
using GitMerger.Jira;

namespace GitMerger.Controllers
{
    public class JiraController : ApiController
    {
        private static readonly XmlDictionaryReaderQuotas InfiniteQuotas = new XmlDictionaryReaderQuotas
        {
            MaxArrayLength = int.MaxValue,
            MaxBytesPerRead = int.MaxValue,
            MaxDepth = int.MaxValue,
            MaxNameTableCharCount = int.MaxValue,
            MaxStringContentLength = int.MaxValue
        };
        public string Get()
        {
            return "nuuuuh";
        }
        public void Post(string json)
        {
            var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), InfiniteQuotas);
            var doc = XDocument.Load(reader);

            string transitionUserName = doc.Root.ElementValue("user", "displayName");
            string transitionUserMail = doc.Root.ElementValue("user", "emailAddress");
            string issueKey = doc.Root.ElementValue("issue", "key");
            string issueSummary = doc.Root.ElementValue("issue", "fields", "summary");
            string issueResolution = doc.Root.ElementValue("issue", "fields", "resolution", "id");

            var issueDetails = new IssueDetails(issueKey, issueSummary)
            {
                Resolution = issueResolution,
            };
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
            // Only trigger merges for "1" ("Fixed")
            if (mergeRequest.IssueDetails.Resolution == "1")
                return true;
            return false;
        }
        private void TriggerMerge(MergeRequest mergeRequest)
        {
            throw new System.NotImplementedException();
        }
    }
}
