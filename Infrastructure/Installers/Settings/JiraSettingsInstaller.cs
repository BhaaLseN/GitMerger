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
                // default value: "1" for "Fixed"
                ValidResolutions = new string[] { "1" };
            }

            #region IJiraSettings Members

            public string[] ValidResolutions { get; set; }

            #endregion
        }
    }
}
