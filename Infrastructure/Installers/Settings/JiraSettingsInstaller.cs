using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Infrastructure.Installers.Settings
{
    public class JiraSettingsInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IJiraSettings>()
                .ImplementedBy<JiraSettings>()
                .Named("jiraSettings")
                .IsFallback()
                .LifeStyle.Singleton);
        }

        #endregion

        internal sealed class JiraSettings : IJiraSettings
        {
            public JiraSettings()
            {
                // default value: "701" for "Resolved -> Closed"
                ValidTransitions = new string[] { "701" };
                // default value: "1" for "Fixed"
                ValidResolutions = new string[] { "1" };
                // default value: "6" for "Closed"
                ClosedStatus = new string[] { "6" };
            }

            #region IJiraSettings Members

            public string[] ValidTransitions { get; set; }
            public string[] ValidResolutions { get; set; }
            public string[] ClosedStatus { get; set; }

            public string DisableAutomergeFieldName { get; set; }
            public string DisableAutomergeFieldValue { get; set; }
            public string BranchFieldName { get; set; }
            public string UpstreamBranchFieldName { get; set; }

            public string BaseUrl { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }

            #endregion
        }
    }
}
