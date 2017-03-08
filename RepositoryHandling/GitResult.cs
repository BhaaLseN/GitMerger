namespace GitMerger.RepositoryHandling
{
    public class GitResult
    {
        public GitResult(GitResultType resultType, string message)
        {
            ResultType = resultType;
            Message = message;
        }
        public GitResult(GitResultType resultType, string messageFormat, params object[] arguments)
            : this(resultType, string.Format(messageFormat, arguments))
        {
        }

        public string Message { get; }
        public GitResultType ResultType { get; }
        public ExecuteResult ExecuteResult { get; set; }
    }

    public enum GitResultType
    {
        Success,
        Failure,
    }
}
