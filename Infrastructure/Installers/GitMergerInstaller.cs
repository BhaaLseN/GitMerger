﻿using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GitMerger.RepositoryHandling;

namespace GitMerger.Infrastructure.Installers
{
    public class GitMergerInstaller : IWindsorInstaller
    {
        #region IWindsorInstaller Members

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component
                .For<IGitMerger>()
                .ImplementedBy<GitMerger.RepositoryHandling.GitMerger>()
                .LifeStyle.Singleton);
            container.Register(Component
                .For<IGitRepositoryManager>()
                .ImplementedBy<GitRepositoryManager>()
                .LifeStyle.Singleton);
        }

        #endregion
    }
}
