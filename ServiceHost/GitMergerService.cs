using System;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace GitMerger
{
    internal partial class GitMergerService : ServiceBase
    {
        private IWebHost _webHost;

        public GitMergerService()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        }

        protected override void OnStart(string[] args)
        {
            var webHostBuilder = CreateWebHostBuilder(args);
            _webHost = webHostBuilder.Build();
            _webHost.Start();

            //string baseAddress = webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);
            string baseAddress = "an address that ASP.NET Core won't tell me (accurately)";
            WriteEventLog(EventLogEntryType.Information, "Listening at {0}", baseAddress);
        }

        protected override void OnStop()
        {
            _webHost?.StopAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }
        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            WriteEventLog(EventLogEntryType.Error, "Unhandled Exception (terminating: {0})\r\n{1}",
                e.IsTerminating, e.ExceptionObject);

            if (e.IsTerminating)
            {
                _webHost?.StopAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();

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
