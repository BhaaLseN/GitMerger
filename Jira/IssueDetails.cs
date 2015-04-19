using System;

namespace GitMerger.Jira
{
    public class IssueDetails
    {
        private readonly string _key;
        private readonly string _summary;

        /// <summary>
        /// Summary for MergeRequestArgs
        /// </summary>
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
        public string Resolution { get; set; }
    }
}
