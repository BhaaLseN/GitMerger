using System;
namespace GitMerger.Git
{
    public class GitResult
    {
        public GitResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
        public GitResult(bool success, string messageFormat, params object[] arguments)
            : this(success, string.Format(messageFormat, arguments))
        {
        }

        public string Message { get; private set; }
        public bool Success { get; private set; }
        public ExecuteResult ExecuteResult { get; set; }
    }
}
