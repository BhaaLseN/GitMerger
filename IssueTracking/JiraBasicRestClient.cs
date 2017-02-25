using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Linq;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.IssueTracking
{
    class JiraBasicRestClient : IJira
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<JiraBasicRestClient>();

        private readonly IJiraSettings _jiraSettings;
        public JiraBasicRestClient(IJiraSettings jiraSettings)
        {
            if (jiraSettings == null)
                throw new ArgumentNullException(nameof(jiraSettings), $"{nameof(jiraSettings)} is null.");
            if (string.IsNullOrEmpty(jiraSettings.BaseUrl))
                throw new ArgumentException($"{nameof(jiraSettings)}.{nameof(jiraSettings.BaseUrl)} must be set to a valid Jira Base URL (ex. http://jira.host.tld:8080/ or http://my.domain.tld/jira/)", nameof(jiraSettings));
            if (string.IsNullOrEmpty(jiraSettings.UserName))
                throw new ArgumentException($"{nameof(jiraSettings)}.{nameof(jiraSettings.UserName)} must be set to a valid Jira Users user name (that has both read permission to your projects and can post comments)", nameof(jiraSettings));
            if (string.IsNullOrEmpty(jiraSettings.Password))
                throw new ArgumentException($"{nameof(jiraSettings)}.{nameof(jiraSettings.Password)} must be set to to the password for jiraSettings.UserName", nameof(jiraSettings));

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

        public void PostComment(string issueKey, string comment)
        {
            var baseUri = new Uri(_jiraSettings.BaseUrl);
            var requestUri = new Uri(baseUri, "rest/api/2/issue/" + issueKey + "/comment");
            var request = WebRequest.CreateHttp(requestUri);
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(_jiraSettings.UserName + ':' + _jiraSettings.Password));
            request.ContentType = "application/json";
            request.Method = "POST";
            try
            {
                using (var requestStream = request.GetRequestStream())
                {
                    var postContent = new XDocument(new XElement("root",
                        new XAttribute("type", "object"),
                        new XElement("body",
                            new XAttribute("type", "string"),
                            comment)));
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(requestStream))
                        postContent.Save(writer);
                }
                var response = request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string responseString = streamReader.ReadToEnd();
                    Logger.Debug(m => m("Response for PostComment: {0}", responseString));
                    var httpResponse = response as HttpWebResponse;
                    if (httpResponse != null && httpResponse.StatusCode == HttpStatusCode.Created)
                    {
                        // TODO: maybe do something else too?
                        Logger.Info(m => m("Comment successfully added to '{0}'.", issueKey));
                    }
                }
            }
            catch (WebException ex)
            {
                using (var streamReader = new StreamReader(ex.Response.GetResponseStream()))
                    Logger.Error(m => m("Request to '{0}' apparently failed: {1}\r\n{2}", requestUri, ex.Message, streamReader.ReadToEnd()), ex);
            }
        }

        #endregion
    }
}
