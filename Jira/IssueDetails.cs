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
        public string TransitionUserName { get; set; }
        public string TransitionUserEMail { get; set; }
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

            string transitionUserName = doc.Root.ElementValue("user", "displayName");
            string transitionUserMail = doc.Root.ElementValue("user", "emailAddress");
            string issueKey = doc.Root.ElementValue("issue", "key");
            string issueSummary = doc.Root.ElementValue("issue", "fields", "summary");
            string issueResolution = doc.Root.ElementValue("issue", "fields", "resolution", "id");
            string issueStatus = doc.Root.ElementValue("issue", "fields", "status", "id");

            return new IssueDetails(issueKey, issueSummary)
            {
                Resolution = issueResolution,
                Status = issueStatus,
                TransitionUserName = transitionUserName,
                TransitionUserEMail = transitionUserMail,
            };
        }
    }
}
