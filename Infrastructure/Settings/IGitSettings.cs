namespace GitMerger.Infrastructure.Settings
{
    public interface IGitSettings
    {
        string GitExecutable { get; }
        string UserName { get; }
        string EMail { get; }
        string RepositoryBasePath { get; }
        RepositoryInfo[] Repositories { get; }
    }
}
