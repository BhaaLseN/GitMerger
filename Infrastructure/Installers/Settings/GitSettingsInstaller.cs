using System;
using System.IO;
using System.Linq;
using Castle.Core.Configuration;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.MicroKernel.SubSystems.Conversion;
using Castle.Windsor;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Infrastructure.Installers.Settings
{
    public class GitSettingsInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            var manager = (IConversionManager)container.Kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey);
            manager.Add(new RepositoryInfoConverter());

            container.Register(Component
                .For<IGitSettings>()
                .ImplementedBy<GitSettings>()
                .Named("gitSettings")
                .IsFallback()
                .LifeStyle.Singleton);
        }

        #endregion
        private sealed class RepositoryInfoConverter : AbstractTypeConverter
        {
            public override bool CanHandleType(Type type)
            {
                return type == typeof(RepositoryInfo);
            }

            public override object PerformConversion(IConfiguration configuration, Type targetType)
            {
                string uri = Get<string>(configuration, "uri");
                string[] schemeAndRest = uri.Split(new char[] { ':' }, 2);
                string relativePath;
                if (schemeAndRest.Length == 2 && schemeAndRest[1].Substring(0, 2) != "//")
                {
                    // no colon, most likely an scp-style git reference
                    relativePath = schemeAndRest[1];
                }
                else
                {
                    // all other uris must have a scheme, or git won't recognize them either
                    var builder = new UriBuilder(uri);
                    relativePath = builder.Path.TrimStart('/');
                }
                return new RepositoryInfo(uri, relativePath);
            }

            public override object PerformConversion(string value, Type targetType)
            {
                throw new NotImplementedException();
            }

            public T Get<T>(IConfiguration configuration, string paramter)
            {
                var setting = configuration.Children.SingleOrDefault(c => c.Name == paramter);
                if (setting == null)
                {
                    throw new ApplicationException(string.Format(
                        "In the castle configuration, type '{0}' expects parameter '{1}' that was missing.",
                        typeof(T).Name, paramter));
                }
                return (T)Context.Composition.PerformConversion(setting, typeof(T));
            }
        }

        internal sealed class GitSettings : IGitSettings
        {
            public GitSettings()
            {
                GitExecutable = "git.exe";
                RepositoryBasePath = Path.Combine(Path.GetDirectoryName(typeof(GitSettings).Assembly.Location), "repositories");
                Repositories = new RepositoryInfo[0];
            }

            #region IGitSettings Members

            public string GitExecutable { get; set; }
            public string RepositoryBasePath { get; set; }
            public RepositoryInfo[] Repositories { get; set; }

            #endregion
        }
    }
}
