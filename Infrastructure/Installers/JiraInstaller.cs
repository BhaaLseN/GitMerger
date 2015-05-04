using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GitMerger.Jira;

namespace GitMerger.Infrastructure.Installers
{
    public class JiraInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IJira>()
                .ImplementedBy<JiraBasicRestClient>()
                .LifeStyle.Singleton);
        }

        #endregion
    }
}
