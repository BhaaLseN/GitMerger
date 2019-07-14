using System;
using Castle.Facilities.AspNetCore;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WConfiguration = Castle.Windsor.Installer.Configuration;

namespace GitMerger
{
    public class Startup
    {
        private static readonly IWindsorContainer _container;

        static Startup()
        {
            _container = new WindsorContainer()
                // app.config first, so we can override registrations if we want/have to
                .Install(WConfiguration.FromXmlFile("gitmerger.config"), FromAssembly.Containing<Startup>());
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            _container.AddFacility<AspNetCoreFacility>(ancf => ancf.CrossWiresInto(services));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            return services.AddWindsor(_container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
