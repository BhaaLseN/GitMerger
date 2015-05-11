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
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<GitRepositoryManager>();

        private readonly IGitSettings _gitSettings;
        public GitRepositoryManager(IGitSettings gitSettings)
        {
            _gitSettings = gitSettings;
        }
        #region IGitRepositoryManager Members

        public IEnumerable<GitRepositoryBranch> FindBranch(string branchName, bool isExactBranchName)
        {
            Logger.Debug(m => m("Trying to find matching repositories for '{0}' (exact match: {1})", branchName, isExactBranchName));
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
                Logger.Debug(m => m("Found {0} branches: {1}", branches.Length, string.Join(", ", branches)));
                if (isExactBranchName)
                {
                    if (branches.Contains(branchName))
                    {
                        Logger.Info(m => m("Got an exact branch name match for '{0}' in '{1}'.",
                            branchName, repository.RepositoryIdentifier));
                        yield return new GitRepositoryBranch(repository, branchName);
                    }
                    // else: no such branch. probably, nothing to do for this repository
                    // TODO: maybe check if theres a matching branch of different casing?
                }
                else
                {
                    string upperBranchName = branchName.ToUpperInvariant();
                    var matchingBranches = branches.Where(branch => branch.ToUpperInvariant().Contains(upperBranchName));
                    if (matchingBranches.Count() == 1)
                    {
                        Logger.Info(m => m("Found a branch name match for '{0}' (exact spelling is '{1}') in '{2}'.",
                            branchName, matchingBranches.First(), repository.RepositoryIdentifier));
                        yield return new GitRepositoryBranch(repository, matchingBranches.First());
                    }
                    else if (matchingBranches.Count() > 1)
                    {
                        Logger.Warn(m => m("Found {0} branches matching '{1}' in repository '{2}', cannot decide which one they wanted: {3}",
                            matchingBranches.Count(), branchName, repository.RepositoryIdentifier, string.Join(", ", matchingBranches)));
                        // TODO: implement notification in case multiple branches are found
                    }
                }
            }
        }

        public GitResult MergeAndPush(GitRepository repository, string branchName, string mergeInto, string mergeAuthor)
        {
            // should never happen, but check anyways
            if (!Exists(repository))
            {
                Logger.Error(m => m("[{0}] Non-existing/initialized repository: Tried to merge branch '{1}' into '{2}'.",
                    repository.RepositoryIdentifier, branchName, mergeInto));
                // TODO: maybe initialize anyways? Not supposed to happen, but meh.
                return new GitResult(false,
                    "Tried to perform a merge on a Non-existing/initialized repository '{0}' (merging '{1}' into '{2}')",
                    repository.RepositoryIdentifier, branchName, mergeInto);
            }

            const string remoteName = "origin"; // TODO: un-const and pass it in?
            var checkoutResult = Git(repository, "checkout --quiet --force {0}", mergeInto);
            if (checkoutResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Checkout failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, checkoutResult.ExitCode,
                    string.Join("\r\n", checkoutResult.StdoutLines), string.Join("\r\n", checkoutResult.StderrLines)));
                return new GitResult(false, "Failed to switch to '{0}' before merge.", mergeInto)
                {
                    ExecuteResult = checkoutResult,
                };
            }

            var pullResult = Git(repository, "pull --quiet --ff --ff-only --no-stat {0} {1}", remoteName, mergeInto);
            if (pullResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Pull failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, pullResult.ExitCode,
                    string.Join("\r\n", pullResult.StdoutLines), string.Join("\r\n", pullResult.StderrLines)));
                return new GitResult(false, "Failed to update '{0}' from '{1}' before merge.", mergeInto, remoteName)
                {
                    ExecuteResult = pullResult,
                };
            }

            var mergeResult = Git(repository, "merge --quiet --no-ff --no-stat {0}/{1}", remoteName, branchName);
            if (mergeResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Merge failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, mergeResult.ExitCode,
                    string.Join("\r\n", mergeResult.StdoutLines), string.Join("\r\n", mergeResult.StderrLines)));

                Git(repository, "merge --abort");
                return new GitResult(false, "Failed to merge '{0}' into '{1}'.", branchName, mergeInto)
                {
                    ExecuteResult = mergeResult,
                };
            }

            var commitResult = Git(repository, "commit --amend --quiet -m \"Merge branch '{0}'\" --author=\"{1}\"",
                branchName, mergeAuthor);
            if (commitResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Commit-amend failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, commitResult.ExitCode,
                    string.Join("\r\n", commitResult.StdoutLines), string.Join("\r\n", commitResult.StderrLines)));
                return new GitResult(false, "Failed to update commit message after merge.")
                {
                    ExecuteResult = commitResult,
                };
            }

            var pushResult = Git(repository, "push --quiet {0} {1}", remoteName, mergeInto);
            if (pushResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Push failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, pushResult.ExitCode,
                    string.Join("\r\n", pushResult.StdoutLines), string.Join("\r\n", pushResult.StderrLines)));
                // TODO: this is probably the place to reset/retry the merge (maybe once, maybe twice if we run into bad luck?)
                return new GitResult(false, "Failed to push branch '{0}' to '{1}' after merge.", mergeInto, remoteName)
                {
                    ExecuteResult = pushResult,
                };
            }

            var pushDeleteResult = Git(repository, "push --quiet --delete {0} {1}", remoteName, branchName);
            if (pushDeleteResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Push-delete failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, pushDeleteResult.ExitCode,
                    string.Join("\r\n", pushDeleteResult.StdoutLines), string.Join("\r\n", pushDeleteResult.StderrLines)));
                // TODO: maybe we want to flag this as success, since its only cleanup? Needs some change on how successful merges deal with the message tho.
                return new GitResult(false, "Failed to delete remote branch '{0}/{1}' after the merge was successful.", remoteName, branchName)
                {
                    ExecuteResult = pushDeleteResult,
                };
            }

            return new GitResult(true, "Successfully merged '{0}' into '{1}' and deleted remote branch '{2}/{0}'.", branchName, mergeInto, remoteName);
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
            Logger.Debug(m => m("Checking whether repository '{0}' exists locally at '{1}' (result is {2}).",
                repository.RepositoryIdentifier, repository.LocalPath, Directory.Exists(repository.LocalPath)));
            return Directory.Exists(repository.LocalPath);
        }
        private bool Initialize(GitRepository repository)
        {
            var cloneResult = Git(null, "clone --quiet \"{0}\" \"{1}\" --config user.name=\"{2}\" --config user.email=\"{3}\"",
                repository.RepositoryIdentifier, repository.LocalPath, _gitSettings.UserName, _gitSettings.EMail);
            if (cloneResult.ExitCode != 0)
            {
                Logger.Error(m => m("[{0}] Clone failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, cloneResult.ExitCode,
                    string.Join("\r\n", cloneResult.StdoutLines), string.Join("\r\n", cloneResult.StderrLines)));
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
                Logger.Error(m => m("[{0}] Fetch failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, fetchResult.ExitCode,
                    string.Join("\r\n", fetchResult.StdoutLines), string.Join("\r\n", fetchResult.StderrLines)));
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
                Logger.Error(m => m("[{0}] Branch list failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                    repository.RepositoryIdentifier, branchResult.ExitCode,
                    string.Join("\r\n", branchResult.StdoutLines), string.Join("\r\n", branchResult.StderrLines)));
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

            Logger.Debug(m => m("Running command '{0}' with command line arguments (working directory is: '{1}'):\r\n{2}",
                p.StartInfo.FileName, p.StartInfo.WorkingDirectory ?? "not set", p.StartInfo.Arguments));

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
            catch (Exception ex)
            {
                Logger.Error(m => m("Running '{0}' failed with exception: {1}", p.StartInfo.FileName, ex.Message), ex);
                if (!p.HasExited)
                    p.Kill();
            }

            return new ExecuteResult(p.ExitCode, stdout, stderr)
            {
                StartInfo = p.StartInfo,
            };
        }
    }
}
