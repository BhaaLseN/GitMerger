using System.Web.Http;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace GitMerger.Infrastructure.Installers
{
    /// <summary>
    /// Registers all controllers for use with the dependency resolver.
    /// </summary>
    /// <remarks>
    /// Apparently, the dependency resolver does not do any IoC on its own and therefore fails on controllers
    /// that do not have a default constructor. Windsor needs to take care of those, so just register them.
    /// WebAPI will call <see cref="IDependencyScope.GetService"/> for pretty much everything, including the controllers.
    /// </remarks>
    public class ControllerInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Classes.FromAssemblyContaining<ControllerInstaller>()
                .BasedOn<ApiController>()
                // scope is based on IDependencyResolver.BeginScope, which seems to be per request
                .LifestyleScoped());
        }

        #endregion
    }
}
