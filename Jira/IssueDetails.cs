using System;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace GitMerger.Jira
{
    public class IssueDetails
    {
        private readonly string _key;
        private readonly string _summary;

        public IssueDetails(string key, string summary)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key", "key is null or empty.");
            if (string.IsNullOrEmpty(summary))
                throw new ArgumentNullException("summary", "summary is null or empty.");

            _key = key;
            _summary = summary;
        }
        public string Key
        {
            get { return _key; }
        }
        public string Summary
        {
            get { return _summary; }
        }
        public bool IsTransition
        {
            get { return !string.IsNullOrWhiteSpace(TransitionId); }
        }
        public string TransitionId { get; set; }
        public string TransitionName { get; set; }
        public string TransitionUserKey { get; set; }
        public string TransitionUserName { get; set; }
        public string TransitionUserEMail { get; set; }
        public string AssigneeUserKey { get; set; }
        public string AssigneeUserName { get; set; }
        public string AssigneeUserEMail { get; set; }
        public string Resolution { get; set; }
        public string Status { get; set; }

        private static readonly XmlDictionaryReaderQuotas InfiniteQuotas = new XmlDictionaryReaderQuotas
        {
            MaxArrayLength = int.MaxValue,
            MaxBytesPerRead = int.MaxValue,
            MaxDepth = int.MaxValue,
            MaxNameTableCharCount = int.MaxValue,
            MaxStringContentLength = int.MaxValue
        };

        public static IssueDetails ParseFromJson(string jsonString)
        {
            var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(jsonString), InfiniteQuotas);
            var doc = XDocument.Load(reader);

            // user information will only be available when a transition is triggered; so lets call it "transition" values
            string transitionUserKey = doc.Root.ElementValue("user", "key");
            // fallback to the name field; which seems to have the same value as key but is present more often
            if (string.IsNullOrWhiteSpace(transitionUserKey))
                transitionUserKey = doc.Root.ElementValue("user", "name");
            string transitionUserName = doc.Root.ElementValue("user", "displayName");
            string transitionUserMail = doc.Root.ElementValue("user", "emailAddress");
            string transitionId = doc.Root.ElementValue("transition", "transitionId");
            string transitionName = doc.Root.ElementValue("transition", "transitionName");

            // issue details might be wrapped in an <issue> element (for the WebHook POST for example),
            // but they might aswell appear directly (for any REST query)
            var issueElement = doc.Root.Element("issue") ?? doc.Root;
            string issueKey = issueElement.ElementValue("key");
            string issueSummary = issueElement.ElementValue("fields", "summary");
            string issueResolution = issueElement.ElementValue("fields", "resolution", "id");
            string issueStatus = issueElement.ElementValue("fields", "status", "id");
            string assigneeUserKey = issueElement.ElementValue("fields", "assignee", "key");
            if (string.IsNullOrWhiteSpace(assigneeUserKey))
                assigneeUserKey = issueElement.ElementValue("fields", "assignee", "name");
            string assigneeUserName = issueElement.ElementValue("fields", "assignee", "displayName");
            string assigneeUserMail = issueElement.ElementValue("fields", "assignee", "emailAddress");

            return new IssueDetails(issueKey, issueSummary)
            {
                Resolution = issueResolution,
                Status = issueStatus,
                TransitionId = transitionId,
                TransitionName = transitionName,
                TransitionUserKey = transitionUserKey,
                TransitionUserName = transitionUserName,
                TransitionUserEMail = transitionUserMail,
                AssigneeUserKey = assigneeUserKey,
                AssigneeUserName = assigneeUserName,
                AssigneeUserEMail = assigneeUserMail,
            };
        }
    }
}
