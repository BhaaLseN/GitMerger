using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;

namespace GitMerger.Infrastructure
{
    public class CastleDependencyResolver : IDependencyResolver
    {
        private readonly IWindsorContainer _container;
        private readonly IDisposable _scope;

        public CastleDependencyResolver(IWindsorContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container", "container is null.");
            _container = container;
        }
        private CastleDependencyResolver(IWindsorContainer container, IDisposable scope)
            : this(container)
        {
            _scope = scope;
        }
        public IDependencyScope BeginScope()
        {
            return new CastleDependencyResolver(_container, _container.BeginScope());
        }

        #region IDependencyScope Members

        public object GetService(Type serviceType)
        {
            return _container.Kernel.HasComponent(serviceType) ? _container.Resolve(serviceType) : null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _container.Kernel.HasComponent(serviceType) ? _container.ResolveAll(serviceType).Cast<object>() : new object[0];
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_scope != null)
                _scope.Dispose();
        }

        #endregion
    }
}
