using System;
using System.IO;
using System.Linq;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.RepositoryHandling
{
    public class GitRepository
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<GitRepository>();

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

            RepositoryIdentifier = repositoryIdentifier;
            LocalPath = localPath;
            _gitSettings = gitSettings;
            _git = new Git(gitSettings);
        }

        public string LocalPath { get; }
        public string RepositoryIdentifier { get; }

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
                LogError(cloneResult, $"Failed to clone repository '{RepositoryIdentifier}' into '{LocalPath}'");
                return false;
            }

            return true;
        }

        public bool Fetch()
        {
            var fetchResult = _git.Execute(LocalPath, "fetch --prune --quiet {0}", RemoteName);
            if (fetchResult.ExitCode != 0)
            {
                LogError(fetchResult, $"Failed to fetch from remote '{RemoteName}'");
                return false;
            }

            return true;
        }

        public string[] Branches()
        {
            var branchResult = _git.Execute(LocalPath, "branch --remotes --list --quiet");
            if (branchResult.ExitCode != 0)
            {
                LogError(branchResult, "Failed to get list of branches");
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

        public bool Checkout(string branch)
        {
            var checkoutResult = _git.Execute(LocalPath, "checkout --quiet --force {0}", branch);
            if (checkoutResult.ExitCode != 0)
            {
                LogError(checkoutResult, $"Checkout of '{branch}' failed");
                return false;
            }

            return true;
        }

        // assumes same "RemoteName" for the branch (defaulting to "origin")
        public bool Pull(string remoteBranch)
        {
            var pullResult = _git.Execute(LocalPath, "pull --quiet --ff --ff-only --no-stat {0} {1}", RemoteName, remoteBranch);
            if (pullResult.ExitCode != 0)
            {
                LogError(pullResult, $"Failed to pull '{RemoteName}/{remoteBranch}'");
                return false;
            }

            return true;
        }

        public string GetHashFor(string rev)
        {
            var revParseResult = _git.Execute(LocalPath, "rev-parse {0}", rev);
            if (revParseResult.ExitCode != 0)
            {
                LogError(revParseResult, $"Failed to get SHA1-hash for rev '{rev}'");
                return null;
            }

            return revParseResult.StdoutLines.FirstOrDefault();
        }

        private void LogError(ExecuteResult executeResult, string message)
        {
            Logger.Error(m => m("[{0}] {1} (Exit Code: {2})\r\nstdout: {3}\r\nstderr: {4}",
                RepositoryIdentifier, message, executeResult.ExitCode,
                string.Join(Environment.NewLine, executeResult.StdoutLines),
                string.Join(Environment.NewLine, executeResult.StderrLines)));
        }

        // TODO: this probably shouldn't be here?
        public GitResult MakeFailureResult(string message)
        {
            return new GitResult(false, message)
            {
                ExecuteResult = _git.LastResult,
            };
        }
    }
}
