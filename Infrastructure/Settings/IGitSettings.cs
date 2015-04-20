namespace GitMerger.Infrastructure.Settings
{
    public interface IGitSettings
    {
        string GitExecutable { get; }
        string RepositoryBasePath { get; }
        RepositoryInfo[] Repositories { get; }
    }
}
