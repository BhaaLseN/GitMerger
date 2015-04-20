using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Git
{
    class GitRepositoryManager : IGitRepositoryManager
    {
        private readonly IGitSettings _gitSettings;
        public GitRepositoryManager(IGitSettings gitSettings)
        {
            _gitSettings = gitSettings;
        }
        #region IGitRepositoryManager Members

        public IEnumerable<GitRepositoryBranch> FindBranch(string branchName, bool isExactBranchName)
        {
            foreach (var repositoryInfo in _gitSettings.Repositories)
            {
                var repository = Get(repositoryInfo);
                if (!Exists(repository))
                {
                    if (!Initialize(repository))
                        continue;
                }
                else
                {
                    if (!Update(repository))
                        continue;
                }

                string[] branches = Branches(repository);
                if (isExactBranchName)
                {
                    if (branches.Contains(branchName))
                        yield return new GitRepositoryBranch(repository, branchName);
                    // else: no such branch. probably, nothing to do for this repository
                    // TODO: maybe check if theres a matching branch of different casing?
                }
                else
                {
                    string upperBranchName = branchName.ToUpperInvariant();
                    var matchingBranches = branches.Where(branch => branch.ToUpperInvariant().Contains(upperBranchName));
                    if (matchingBranches.Count() == 1)
                        yield return new GitRepositoryBranch(repository, matchingBranches.First());
                    // else: if theres more than one, notify about the inability to choose the right one.
                    // TODO: implement notification in that case
                }
            }
        }

        public bool MergeAndPush(GitRepository repository, string branchName, string mergeInto)
        {
            // should never happen, but check anyways
            if (!Exists(repository))
                return false;

            var checkoutResult = Git(repository, "checkout --quiet --force {0}", mergeInto);
            if (checkoutResult.ExitCode != 0)
            {
                // TODO: log somewhere, or notify someone.
                return false;
            }

            var pullResult = Git(repository, "pull --quiet --ff --ff-only --no-stat origin {0}", mergeInto);
            if (pullResult.ExitCode != 0)
            {
                // TODO: log somewhere, or notify someone.
                return false;
            }

            var mergeResult = Git(repository, "merge --quiet --no-ff --no-stat -m \"Merge branch '{0}'\" origin/{0}", branchName);
            if (mergeResult.ExitCode != 0)
            {
                Git(repository, "merge --abort");
                // TODO: log somewhere, or notify someone.
                return false;
            }

            var pushResult = Git(repository, "push --quiet origin {0}", mergeInto);
            if (pushResult.ExitCode != 0)
            {
                // TODO: log somewhere, or notify someone.
                return false;
            }

            return true;
        }

        #endregion

        private GitRepository Get(RepositoryInfo repositoryInfo)
        {
            string localPath = Path.GetFullPath(Path.Combine(_gitSettings.RepositoryBasePath, repositoryInfo.RelativePath));
            if (localPath.EndsWith(".git", StringComparison.InvariantCultureIgnoreCase))
                localPath = localPath.Substring(0, localPath.Length - 4);
            var repo = new GitRepository(repositoryInfo.OriginalString, localPath);
            return repo;
        }
        private bool Exists(GitRepository repository)
        {
            return Directory.Exists(repository.LocalPath);
        }
        private bool Initialize(GitRepository repository)
        {
            var cloneResult = Git(repository, "clone --quiet \"{0}\" \"{1}\"", repository.RepositoryIdentifier, repository.LocalPath);
            if (cloneResult.ExitCode != 0)
            {
                // TODO: log somewhere, or notify someone.
                return false;
            }

            return true;
        }
        private bool Update(GitRepository repository)
        {
            var fetchResult = Git(repository, "fetch --prune --quiet origin");
            if (fetchResult.ExitCode != 0)
            {
                // TODO: log somewhere, or notify someone.
                return false;
            }

            return true;
        }
        private string[] Branches(GitRepository repository)
        {
            var branchResult = Git(repository, "branch --remotes --list --quiet");
            if (branchResult.ExitCode != 0)
            {
                // TODO: log somewhere, or notify someone.
                return new string[0];
            }
            return branchResult.StdoutLines
                .Select(l => l.Trim())
                // only return branches from our remote; but none that are only just refs
                .Where(l => l.StartsWith("origin/") && !l.Contains(" -> "))
                .Select(l => l.Substring("origin/".Length))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
        }

        private ExecuteResult Git(GitRepository repository, string format, params object[] args)
        {
            string workingDirectory = null;
            if (repository != null)
                workingDirectory = repository.LocalPath;

            string arguments = string.Format(format, args);
            var p = new Process
            {
                StartInfo = new ProcessStartInfo(_gitSettings.GitExecutable, arguments)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false,
                    WorkingDirectory = workingDirectory,
                }
            };
            var stdout = new List<string>();
            var stderr = new List<string>();

            var stdoutEvent = new ManualResetEvent(false);
            var stderrEvent = new ManualResetEvent(false);
            var exited = new ManualResetEvent(false);
            p.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    stdoutEvent.Set();
                else
                    stdout.Add(e.Data);
            };
            p.ErrorDataReceived += (s, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                    stderrEvent.Set();
                else
                    stderr.Add(e.Data);
            };
            p.Exited += (s, e) => exited.Set();
            p.EnableRaisingEvents = true;

            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                WaitHandle.WaitAll(new[] { stdoutEvent, stderrEvent, exited });
            }
            catch (Exception)
            {
                if (!p.HasExited)
                    p.Kill();
            }

            return new ExecuteResult(p.ExitCode, stdout, stderr);
        }
    }
}
