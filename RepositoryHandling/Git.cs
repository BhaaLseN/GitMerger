using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.RepositoryHandling
{
    public class Git
    {
        private static readonly global::Common.Logging.ILog Logger = global::Common.Logging.LogManager.GetLogger<Git>();

        private readonly IGitSettings _gitSettings;

        public Git(IGitSettings gitSettings)
        {
            if (gitSettings == null)
                throw new ArgumentNullException(nameof(gitSettings), $"{nameof(gitSettings)} is null.");
            _gitSettings = gitSettings;
        }

        public ExecuteResult Execute(string workingDirectory, string format, params object[] args)
        {
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
