using BareboneDI.Enums;
using System;

namespace BareboneDI.Core
{
    /// <summary>
    /// Stores registration details such as the implementation type, lifetime, and an optional factory delegate.
    /// </summary>
    internal class Registration
    {
        public Type ImplementationType { get; private set; }
        public Lifetime Lifetime { get; private set; }
        public object SingletonInstance { get; set; }

        /// <summary>
        /// Optional factory delegate used for creating the instance.
        /// If set, this delegate is invoked instead of using constructor injection.
        /// </summary>
        public Func<DependencyContainer, object> Factory { get; set; }

        public Registration(Type implementationType, Lifetime lifetime)
        {
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }
    }
}
