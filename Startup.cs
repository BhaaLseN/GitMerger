using System.Web.Http;
using Castle.Windsor;
using Castle.Windsor.Installer;
using GitMerger.Infrastructure;
using GitMerger.Infrastructure.Settings;
using Owin;
using WConfiguration = Castle.Windsor.Installer.Configuration;

namespace GitMerger
{
    internal class Startup
    {
        private static readonly IWindsorContainer _container;
        private static readonly IHostSettings _hostSettings;

        public static IHostSettings HostSettings
        {
            get { return _hostSettings; }
        }

        static Startup()
        {
            _container = new WindsorContainer()
                // app.config first, so we can override registrations if we want/have to
                .Install(WConfiguration.FromAppConfig(), FromAssembly.This());
            _hostSettings = _container.Resolve<IHostSettings>();
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
