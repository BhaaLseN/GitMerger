using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Infrastructure.Installers.Settings
{
    public class HostSettingsInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IHostSettings>()
                .ImplementedBy<HostSettings>()
                .Named("hostSettings")
                .IsFallback()
                .LifeStyle.Singleton);
        }

        #endregion

        internal sealed class HostSettings : IHostSettings
        {
            public HostSettings()
            {
                BaseAddress = "http://localhost:12345/test";
            }
            #region IHostSettings Members

            public string BaseAddress { get; set; }

            #endregion
        }
    }
}
