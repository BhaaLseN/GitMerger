using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Jira
{
    class JiraBasicRestClient : IJira
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<JiraBasicRestClient>();

        private readonly IJiraSettings _jiraSettings;
        public JiraBasicRestClient(IJiraSettings jiraSettings)
        {
            if (jiraSettings == null)
                throw new ArgumentNullException("jiraSettings", "jiraSettings is null.");
            if (string.IsNullOrEmpty(jiraSettings.BaseUrl))
                throw new ArgumentException("jiraSettings.BaseUrl must be set to a valid Jira Base URL (ex. http://jira.host.tld:8080/ or http://my.domain.tld/jira/)", "jiraSettings");
            if (string.IsNullOrEmpty(jiraSettings.UserName))
                throw new ArgumentException("jiraSettings.UserName must be set to a valid Jira Users user name (that has both read permission to your projects and can post comments)", "jiraSettings");
            if (string.IsNullOrEmpty(jiraSettings.Password))
                throw new ArgumentException("jiraSettings.Password must be set to to the password for jiraSettings.UserName", "jiraSettings");

            _jiraSettings = jiraSettings;
        }
        #region IJira Members

        public IssueDetails GetIssueDetails(string issueKey)
        {
            var baseUri = new Uri(_jiraSettings.BaseUrl);
            var requestUri = new Uri(baseUri, "rest/api/2/issue/" + issueKey);
            var request = WebRequest.CreateHttp(requestUri);
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(_jiraSettings.UserName + ':' + _jiraSettings.Password));
            request.Accept = "application/json";
            request.Method = "GET";
            try
            {
                var response = request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = streamReader.ReadToEnd();
                    Logger.Debug(m => m("Response for IssueDetails: {0}", responseString));
                    var httpResponse = response as HttpWebResponse;
                    if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        // TODO: maybe check for Json?
                        return IssueDetails.ParseFromJson(responseString);
                    }
                }
            }
            catch (WebException ex)
            {
                using (var streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    Logger.Error(m => m("Request to '{0}' apparently failed: {1}\r\n{2}", requestUri, ex.Message, streamReader.ReadToEnd()), ex);
            }
            return null;
        }

        public bool IsClosed(IssueDetails issueDetails)
        {
            if (issueDetails == null)
                throw new ArgumentNullException("issueDetails", "issueDetails is null.");

            return _jiraSettings.ClosedStatus.Contains(issueDetails.Status);
        }

        #endregion
    }
}
