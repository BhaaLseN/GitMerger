using System;
using System.Web.Http;
using Castle.Windsor;
using Castle.Windsor.Installer;
using GitMerger.Infrastructure;
using GitMerger.Infrastructure.Settings;
using Microsoft.Owin.Hosting;
using Owin;
using WConfiguration = Castle.Windsor.Installer.Configuration;

namespace GitMerger
{
    public class Startup
    {
        private static readonly IWindsorContainer _container;
        private static readonly IHostSettings _hostSettings;
        private static IDisposable _app;
        private static bool _isDisposed;

        static Startup()
        {
            _container = new WindsorContainer()
                // app.config first, so we can override registrations if we want/have to
                .Install(WConfiguration.FromAppConfig(), FromAssembly.This());
            _hostSettings = _container.Resolve<IHostSettings>();
        }

        /// <summary>
        /// Starts a WebApi Service and returns the base address used.
        /// </summary>
        /// <returns>The base address of the service</returns>
        public static string Start()
        {
            _app = WebApp.Start<Startup>(_hostSettings.BaseAddress);
            return _hostSettings.BaseAddress;
        }

        public static void Shutdown()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (_app != null)
                {
                    _app.Dispose();
                    _app = null;
                }
                _container.Dispose();
            }
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
