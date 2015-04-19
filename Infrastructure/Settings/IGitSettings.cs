using System;

namespace GitMerger.Infrastructure.Settings
{
    public interface IGitSettings
    {
        string GitExecutable { get; }
        string RepositoryBasePath { get; }
        Uri[] RepositoryUrls { get; }
    }
}
