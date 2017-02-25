namespace GitMerger.RepositoryHandling
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

        public string Message { get; }
        public bool Success { get; }
        public ExecuteResult ExecuteResult { get; set; }
    }
}
