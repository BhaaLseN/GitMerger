using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.RepositoryHandling
{
    class GitRepositoryManager : IGitRepositoryManager
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<GitRepositoryManager>();

        private readonly IGitSettings _gitSettings;
        private readonly Git _git;

        public GitRepositoryManager(IGitSettings gitSettings)
        {
            _gitSettings = gitSettings;
            _git = new Git(_gitSettings);
        }

        #region IGitRepositoryManager Members

        public IEnumerable<GitRepositoryBranch> FindBranch(string branchName, bool isExactBranchName)
        {
            Logger.Debug(m => m("Trying to find matching repositories for '{0}' (exact match: {1})", branchName, isExactBranchName));
            foreach (var repositoryInfo in _gitSettings.Repositories)
            {
                var repository = Get(repositoryInfo);
                if (!repository.Exists())
                {
                    if (!repository.Initialize())
                        // TODO: notify someone that the repository couldn't be initialized?
                        continue;
                }
                else
                {
                    if (!repository.Fetch())
                        // TODO: notify someone that the fetch failed?
                        continue;
                }

                string[] branches = repository.Branches();
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
                    // since its not an exact match, we can assume it is a Jira issue key.
                    // they follow the pattern "<project key>-<issue number>", where the issue number is strictly numeric;
                    // and the project key only contains letters, numbers or the underscore (always starting with a letter).
                    // append a negative look-ahead for digits, which should prevent matching partial issue numbers.
                    // we cannot use word-boundaries here, since it will not match names such as JRA-123b or JRA-123_fixed.
                    // NOTE: this is mostly convention being used in practise; this might not match all configurations on all systems.
                    // FIXME: this does not account for the beginning of the branch name; but in practise we do not have overlap
                    //        (and therefore don't need to check for it right now)
                    var jiraIssueKey = new Regex(branchName + @"(?!\d)", RegexOptions.IgnoreCase);
                    var matchingBranches = branches.Where(branch => jiraIssueKey.IsMatch(branch));
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
            if (!repository.Exists())
            {
                Logger.Error(m => m("[{0}] Non-existing/initialized repository: Tried to merge branch '{1}' into '{2}'.",
                    repository.RepositoryIdentifier, branchName, mergeInto));
                // TODO: maybe initialize anyways? Not supposed to happen, but meh.
                return repository.MakeFailureResult(string.Format(
                    "Tried to perform a merge on a Non-existing/initialized repository '{0}' (merging '{1}' into '{2}')",
                    repository.RepositoryIdentifier, branchName, mergeInto));
            }

            string remoteName = repository.RemoteName;

            if (!repository.Checkout(mergeInto))
                return repository.MakeFailureResult($"Failed to switch to branch '{mergeInto}' before merge.");

            // retry at most 3 times. having bad luck once during the push is one thing, but twice is unlikely.
            // more than that is probably not worth it and will just take up extra time we don't want to waste here.
            const int maxRetryCount = 3;
            int retryCount = maxRetryCount;
            // not an actual infinite loop; it will only run multiple times when the push fails.
            // everything else will exit the loop and method at the same time with a return.
            while (true)
            {
                if (!repository.Pull(mergeInto))
                    return repository.MakeFailureResult($"Failed to update '{mergeInto}' from '{remoteName}' before merge.");

                string oldHeadRev = repository.GetHashFor("HEAD");
                if (string.IsNullOrWhiteSpace(oldHeadRev))
                    return repository.MakeFailureResult("Pre-Merge: Failed to retrieve current HEAD. This is probably a bad thing and needs to be fixed by a human.");

                if (!repository.Merge(branchName))
                {
                    repository.MergeAbort();
                    return repository.MakeFailureResult($"Failed to merge '{branchName}' into '{mergeInto}'.");
                }

                string newHeadRef = repository.GetHashFor("HEAD");
                if (string.IsNullOrWhiteSpace(newHeadRef))
                {
                    repository.MergeAbort();
                    return repository.MakeFailureResult("Post-Merge: Failed to retrieve current HEAD. This is probably a bad thing and needs to be fixed by a human.");
                }

                bool branchAlreadyMerged = newHeadRef == oldHeadRev;
                if (branchAlreadyMerged)
                {
                    Logger.Info(m => m("[{0}] Merge did not create a new commit; this branch has been merged already.", repository.RepositoryIdentifier));
                }
                else
                {
                    var commitResult = _git.Execute(repository.LocalPath, "commit --amend --quiet -m \"Merge branch '{0}'\" --author=\"{1}\"",
                        branchName, mergeAuthor);
                    if (commitResult.ExitCode != 0)
                    {
                        Logger.Error(m => m("[{0}] Commit-amend failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                            repository.RepositoryIdentifier, commitResult.ExitCode,
                            string.Join(Environment.NewLine, commitResult.StdoutLines),
                            string.Join(Environment.NewLine, commitResult.StderrLines)));
                        return new GitResult(false, "Failed to update commit message after merge.")
                        {
                            ExecuteResult = commitResult,
                        };
                    }

                    var pushResult = _git.Execute(repository.LocalPath, "push --quiet {0} {1}", remoteName, mergeInto);
                    if (pushResult.ExitCode != 0)
                    {
                        Logger.Error(m => m("[{0}] Push failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                            repository.RepositoryIdentifier, pushResult.ExitCode,
                            string.Join(Environment.NewLine, pushResult.StdoutLines),
                            string.Join(Environment.NewLine, pushResult.StderrLines)));

                        // see if reached our retry count; if not we'll just try again under the assumption someone else pushed to the
                        // remote in the meantime.
                        if (retryCount --> 0)
                        {
                            // TODO: technically we should check if git told us to "fetch first" (ie. our merge simply had bad timing and
                            //       someone else push their changes in the meantime. but since we only retry 3 times, it's easier to just ignore that.
                            Logger.Info(m => m("Failed to push the merge; retrying {0}/{1}", (maxRetryCount - retryCount), maxRetryCount));

                            // try to reset our target branch back; or we might run into issues when trying to re-do the merge
                            var resetResult = _git.Execute(repository.LocalPath, "reset --hard --quiet {0}/{1}", remoteName, mergeInto);
                            if (resetResult.ExitCode == 0)
                            {
                                // add some wait time before retrying. the first retry will wait for 0 seconds (which effectively turns into a yield)
                                // while all following retries wait for 5 seconds times the retry counter (so, 0s -> 5s -> 10s at most).
                                Thread.Sleep(TimeSpan.FromSeconds((maxRetryCount - retryCount - 1) * 5));
                                continue;
                            }
                        }

                        return new GitResult(false, "Failed to push branch '{0}' to '{1}' after merge. This was attempted {2} times, all of them unsuccessful.", mergeInto, remoteName, maxRetryCount)
                        {
                            ExecuteResult = pushResult,
                        };
                    }
                }

                var pushDeleteResult = _git.Execute(repository.LocalPath, "push --quiet --delete {0} {1}", remoteName, branchName);
                if (pushDeleteResult.ExitCode != 0)
                {
                    Logger.Warn(m => m("[{0}] Push-delete failed with exit code {1}\r\nstdout: {2}\r\nstderr: {3}",
                        repository.RepositoryIdentifier, pushDeleteResult.ExitCode,
                        string.Join(Environment.NewLine, pushDeleteResult.StdoutLines),
                        string.Join(Environment.NewLine, pushDeleteResult.StderrLines)));
                    string message;
                    if (branchAlreadyMerged)
                        message = string.Format("Branch '{0}' was already merged, and deleting the remote branch '{1}/{0}' failed.", branchName, remoteName);
                    else
                        message = string.Format("Successfully merged '{0}' into '{1}', but deleting the remote branch '{2}/{0}' failed.", branchName, mergeInto, remoteName);
                    return new GitResult(true, message)
                    {
                        ExecuteResult = pushDeleteResult,
                    };
                }

                if (branchAlreadyMerged)
                    return new GitResult(true, "Branch '{0}' was already merged, but we deleted the left-over remote branch '{1}/{0}' for you.", branchName, remoteName);
                else
                    return new GitResult(true, "Successfully merged '{0}' into '{1}' and deleted remote branch '{2}/{0}'.", branchName, mergeInto, remoteName);
            }
        }

        #endregion

        private GitRepository Get(RepositoryInfo repositoryInfo)
        {
            string localPath = Path.GetFullPath(Path.Combine(_gitSettings.RepositoryBasePath, repositoryInfo.RelativePath));
            if (localPath.EndsWith(".git", StringComparison.InvariantCultureIgnoreCase))
                localPath = localPath.Substring(0, localPath.Length - 4);
            var repo = new GitRepository(repositoryInfo.OriginalString, localPath, _gitSettings);
            return repo;
        }
    }
}
