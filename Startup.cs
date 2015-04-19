using System.Web.Http;
using Castle.Windsor;
using Castle.Windsor.Installer;
using GitMerger.Infrastructure;
using Owin;

namespace GitMerger
{
    internal class Startup
    {
        private static readonly IWindsorContainer _container;

        static Startup()
        {
            _container = new WindsorContainer()
                .Install(FromAssembly.This());
        }

        public static void Shutdown()
        {
            _container.Dispose();
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("GitMerger", "{controller}");
            config.DependencyResolver = new CastleDependencyResolver(_container);
            appBuilder.UseWebApi(config);
        }
    }
}
