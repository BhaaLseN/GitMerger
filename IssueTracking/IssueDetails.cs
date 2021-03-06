using System;
using System.Collections.Generic;
using System.Linq;

namespace GitMerger.IssueTracking
{
    public class IssueDetails
    {
        public IssueDetails(string key, string summary)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key), $"{nameof(key)} is null or empty.");
            if (string.IsNullOrEmpty(summary))
                throw new ArgumentNullException(nameof(summary), $"{nameof(summary)} is null or empty.");

            Key = key;
            Summary = summary;
        }

        public string Key { get; }
        public string Summary { get; }
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
        public ILookup<string, string> CustomFields { get; set; }

        public static IssueDetails ParseFromJson(string jsonString)
        {
            var doc = JsonHelper.DeserializeFrom(jsonString);

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

            // gather custom fields; someone might need them
            var fields = issueElement.Element("fields");
            var customFields = new List<Tuple<string, string>>();
            if (fields != null)
            {
                foreach (var customField in fields.Elements().Where(e => e.Name.LocalName.StartsWith("customfield_")))
                {
                    string customFieldName = customField.Name.LocalName;
                    string dataType = customField.AttributeValue("type");
                    if (dataType == "null")
                    {
                        // most simple case: the field is not set - do not add it to the list at all
                        continue;
                    }
                    else if (dataType == "array")
                    {
                        // field is an array: assume the contents are simple items that have a value each
                        foreach (var item in customField.Elements("item"))
                        {
                            customFields.Add(new Tuple<string, string>(customFieldName, item.ElementValue("value")));
                        }
                    }
                    else
                    {
                        // everything else: assume it is a simple type where the value is the string-content of the element
                        customFields.Add(new Tuple<string, string>(customFieldName, customField.Value));
                    }
                }
            }

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
                CustomFields = customFields.ToLookup(k => k.Item1, v => v.Item2),
            };
        }
    }
}
