using System;

namespace GitMerger.Infrastructure.Settings
{
    public interface IGitSettings
    {
        string GitExecutable { get; }
        string UserName { get; }
        string EMail { get; }
        TimeSpan MergeDelay { get; }
        string RepositoryBasePath { get; }
        RepositoryInfo[] Repositories { get; }
        string IgnoredBranchPattern { get; }
    }
}
