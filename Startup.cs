using System.Web.Http;
using Owin;

namespace GitMerger
{
    class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("GitMerger", "{controller}");
            appBuilder.UseWebApi(config);
        }
    }
}
