using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GitMerger.RepositoryHandling
{
    public class ExecuteResult
    {
        public ExecuteResult(int exitCode, IEnumerable<string> stdoutLines, IEnumerable<string> stderrLines)
        {
            ExitCode = exitCode;
            StdoutLines = stdoutLines.ToArray();
            StderrLines = stderrLines.ToArray();
        }
        public ProcessStartInfo StartInfo { get; set; }
        public int ExitCode { get; private set; }
        public string[] StdoutLines { get; private set; }
        public string[] StderrLines { get; private set; }
    }
}
