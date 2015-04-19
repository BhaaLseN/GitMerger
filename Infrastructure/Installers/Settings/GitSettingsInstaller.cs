using System;
using System.IO;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GitMerger.Infrastructure.Settings;

namespace GitMerger.Infrastructure.Installers.Settings
{
    public class GitSettingsInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IGitSettings>()
                .ImplementedBy<GitSettings>()
                .Named("gitSettings")
                .IsFallback()
                .LifeStyle.Singleton);
        }

        #endregion

        internal sealed class GitSettings : IGitSettings
        {
            public GitSettings()
            {
                GitExecutable = "git.exe";
                RepositoryBasePath = Path.Combine(Path.GetDirectoryName(typeof(GitSettings).Assembly.Location), "repositories");
                RepositoryUrls = new Uri[0];
            }

            #region IGitSettings Members

            public string GitExecutable { get; set; }
            public string RepositoryBasePath { get; set; }
            public Uri[] RepositoryUrls { get; set; }

            #endregion
        }
    }
}
