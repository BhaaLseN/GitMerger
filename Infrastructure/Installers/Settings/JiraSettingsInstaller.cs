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
                // default value: "10001" for "Done"
                ClosedStatus = new string[] { "10001" };
            }

            #region IJiraSettings Members

            public string[] ValidTransitions { get; set; }
            public string[] ValidResolutions { get; set; }
            public string[] ClosedStatus { get; set; }

            public string BaseUrl { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }

            #endregion
        }
    }
}
