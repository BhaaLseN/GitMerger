using System;
using System.IO;
using System.Linq;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.RepositoryHandling
{
    public class GitRepository
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<GitRepository>();

        private readonly string _localPath;
        private readonly string _repositoryIdentifier;
        private readonly IGitSettings _gitSettings;
        private readonly Git _git;

        // TODO: un-const and pass it in?
        public readonly string RemoteName = "origin";

        public GitRepository(string repositoryIdentifier, string localPath, IGitSettings gitSettings)
        {
            if (string.IsNullOrEmpty(repositoryIdentifier))
                throw new ArgumentNullException(nameof(repositoryIdentifier), $"{nameof(repositoryIdentifier)} is null or empty.");
            if (string.IsNullOrEmpty(localPath))
                throw new ArgumentNullException(nameof(localPath), $"{nameof(localPath)} is null or empty.");
            if (gitSettings == null)
                throw new ArgumentNullException(nameof(gitSettings), $"{nameof(gitSettings)} is null.");

            _repositoryIdentifier = repositoryIdentifier;
            _localPath = localPath;
            _gitSettings = gitSettings;
            _git = new Git(gitSettings);
        }

        public string LocalPath
        {
            get { return _localPath; }
        }
        public string RepositoryIdentifier
        {
            get { return _repositoryIdentifier; }
        }

        public bool Exists()
        {
            Logger.Debug(m => m("Checking whether repository '{0}' exists locally at '{1}' (result is {2}).",
                RepositoryIdentifier, LocalPath, Directory.Exists(LocalPath)));
            return Directory.Exists(LocalPath);
        }

        public bool Initialize()
        {
            var cloneResult = _git.Execute(null, "clone --quiet \"{0}\" \"{1}\" --config user.name=\"{2}\" --config user.email=\"{3}\"",
                RepositoryIdentifier, LocalPath, _gitSettings.UserName, _gitSettings.EMail);
            if (cloneResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Clone failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    RepositoryIdentifier, cloneResult.ExitCode,
                    string.Join(Environment.NewLine, cloneResult.StdoutLines),
                    string.Join(Environment.NewLine, cloneResult.StderrLines)));
                return false;
            }

            return true;
        }

        public bool Fetch()
        {
            var fetchResult = _git.Execute(LocalPath, "fetch --prune --quiet {0}", RemoteName);
            if (fetchResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Fetch failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    RepositoryIdentifier, fetchResult.ExitCode,
                    string.Join(Environment.NewLine, fetchResult.StdoutLines),
                    string.Join(Environment.NewLine, fetchResult.StderrLines)));
                return false;
            }

            return true;
        }

        public string[] Branches()
        {
            var branchResult = _git.Execute(LocalPath, "branch --remotes --list --quiet");
            if (branchResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Branch list failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    RepositoryIdentifier, branchResult.ExitCode,
                    string.Join(Environment.NewLine, branchResult.StdoutLines),
                    string.Join(Environment.NewLine, branchResult.StderrLines)));
                return new string[0];
            }
            return branchResult.StdoutLines
                .Select(l => l.Trim())
                // only return branches from our remote; but none that are only just refs
                .Where(l => l.StartsWith($"{RemoteName}/") && !l.Contains(" -> "))
                .Select(l => l.Substring($"{RemoteName}/".Length))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
        }
    }
}
