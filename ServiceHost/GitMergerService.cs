using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace GitMerger
{
    partial class GitMergerService : ServiceBase
    {
        public GitMergerService()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        }

        protected override void OnStart(string[] args)
        {
            string baseAddress = Startup.Start();
            WriteEventLog(EventLogEntryType.Information, "Listening at {0}", baseAddress);
        }

        protected override void OnStop()
        {
            Startup.Shutdown();
        }
        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteEventLog(EventLogEntryType.Error, "Unhandled Exception (terminating: {0})\r\n{1}",
                e.IsTerminating, e.ExceptionObject);

            if (e.IsTerminating)
            {
                Startup.Shutdown();
            }
        }

        private const int MaxEventLogMessageLength = 32765;
        private void WriteEventLog(EventLogEntryType entryType, string message, params object[] args)
        {
            string exceptionEntry = string.Format(message, args ?? new object[0]);

            if (exceptionEntry.Length > MaxEventLogMessageLength)
                exceptionEntry = exceptionEntry.Substring(0, MaxEventLogMessageLength);
            EventLog.WriteEntry(exceptionEntry, entryType);
        }
    }
}
