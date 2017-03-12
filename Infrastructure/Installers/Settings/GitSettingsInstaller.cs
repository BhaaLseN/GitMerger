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
                string relativePath = null;
                if (schemeAndRest.Length == 2)
                {
                    // see if we match something that isn't a scheme. leave relativePath as null if nothing in here matches
                    if (schemeAndRest[0].Length == 1 && char.IsLetter(schemeAndRest[0][0]))
                        // looks like a rooted windows file path without file:// scheme; lets assume its a path
                        // Path.GetFileName returns the last segment; for a folder path this is the last folder name
                        relativePath = Path.GetFileName(uri.TrimEnd('/', '\\'));
                    else if (schemeAndRest[1].Substring(0, 2) != "//")
                        // no colon, most likely an scp-style git reference
                        relativePath = schemeAndRest[1];
                }

                if (string.IsNullOrEmpty(relativePath))
                {
                    if (!uri.Contains(':'))
                    {
                        // not a single colon? assume this is some sort of path (relative or linux)
                        relativePath = Path.GetFileName(uri.TrimEnd('/', '\\'));
                    }
                    else
                    {
                        // all other uris must have a scheme, or git won't recognize them either
                        var builder = new UriBuilder(uri);
                        relativePath = builder.Path.TrimStart('/');
                    }
                }
                return new RepositoryInfo(uri, relativePath);
            }

            public override object PerformConversion(string value, Type targetType)
            {
                throw new NotImplementedException();
            }

            public T Get<T>(IConfiguration configuration, string parameter)
            {
                var setting = configuration.Children.SingleOrDefault(c => c.Name == parameter);
                if (setting == null)
                {
                    throw new ApplicationException(string.Format(
                        "In the castle configuration, type '{0}' expects parameter '{1}' that was missing.",
                        typeof(T).Name, parameter));
                }
                return (T)Context.Composition.PerformConversion(setting, typeof(T));
            }
        }

        internal sealed class GitSettings : IGitSettings
        {
            public GitSettings()
            {
                GitExecutable = "git.exe";
                UserName = "Git Automerger";
                EMail = "automerge@domain.tld";
                RepositoryBasePath = Path.Combine(Path.GetDirectoryName(typeof(GitSettings).Assembly.Location), "repositories");
                Repositories = new RepositoryInfo[0];
                MergeDelay = TimeSpan.FromMinutes(5);
                IgnoredBranchPattern = @"^private/|^review/|^test(?:ing)?/";
            }

            #region IGitSettings Members

            public string GitExecutable { get; set; }
            public string UserName { get; set; }
            public string EMail { get; set; }
            public TimeSpan MergeDelay { get; set; }
            public string RepositoryBasePath { get; set; }
            public RepositoryInfo[] Repositories { get; set; }
            public string IgnoredBranchPattern { get; set; }

            #endregion

        }
    }
}
