using BareboneDI.Enums;
using BareboneDI.Interfaces;
using System.Collections.Generic;

namespace BareboneDI.Core
{
    /// <summary>
    /// Represents a child container (lifetime scope) that caches scoped instances.
    /// </summary>
    public class LifetimeScope : IDependencyContainer
    {
        private readonly DependencyContainer _parentContainer;
        private readonly Dictionary<Registration, object> _scopedInstances = new Dictionary<Registration, object>();

        public LifetimeScope(DependencyContainer parent)
        {
            _parentContainer = parent;
        }

        /// <summary>
        /// Tries to get a cached scoped instance for the given registration.
        /// </summary>
        internal bool TryGetScopedInstance(Registration registration, out object instance)
        {
            return _scopedInstances.TryGetValue(registration, out instance);
        }

        /// <summary>
        /// Caches a newly created scoped instance.
        /// </summary>
        internal void AddScopedInstance(Registration registration, object instance)
        {
            _scopedInstances[registration] = instance;
        }

        public void Register<TService, TImplementation>() where TImplementation : TService
        {
            _parentContainer.Register<TService, TImplementation>();
        }

        public void Register<TService, TImplementation>(Lifetime lifetime) where TImplementation : TService
        {
            _parentContainer.Register<TService, TImplementation>(lifetime);
        }

        public void Register<TService, TImplementation>(Lifetime lifetime, object key) where TImplementation : TService
        {
            _parentContainer.Register<TService, TImplementation>(lifetime, key);
        }

        public TService Resolve<TService>()
        {
            return (TService)_parentContainer.Resolve(typeof(TService), this);
        }

        public TService Resolve<TService>(object key)
        {
            return (TService)_parentContainer.Resolve(typeof(TService), this, key);
        }
    }
}
