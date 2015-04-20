using System;

namespace GitMerger.Git
{
    public class GitRepository
    {
        private readonly string _localPath;
        private readonly string _repositoryIdentifier;
        public GitRepository(string repositoryIdentifier, string localPath)
        {
            if (string.IsNullOrEmpty(repositoryIdentifier))
                throw new ArgumentNullException("repositoryIdentifier", "repositoryIdentifier is null or empty.");
            if (string.IsNullOrEmpty(localPath))
                throw new ArgumentNullException("localPath", "localPath is null or empty.");
            _repositoryIdentifier = repositoryIdentifier;
            _localPath = localPath;
        }
        public string LocalPath
        {
            get { return _localPath; }
        }
        public string RepositoryIdentifier
        {
            get { return _repositoryIdentifier; }
        }
    }
}
