using BareboneDI.Attributes;
using BareboneDI.Enums;
using BareboneDI.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BareboneDI.Core
{
    /// <summary>
    /// A lightweight DI container that supports lifetime management, open generic registrations,
    /// property injection, keyed registrations, lifetime scopes, auto-registration, delegate/factory registrations,
    /// basic interception stubs, parameter overrides, and collection resolution.
    /// </summary>
    public class DependencyContainer : IDependencyContainer
    {
        // Keyless registrations.
        private readonly Dictionary<Type, Registration> _registrations = new Dictionary<Type, Registration>();

        // Keyed registrations: for a given service type, a dictionary holding registrations by key.
        private readonly Dictionary<Type, Dictionary<object, Registration>> _keyedRegistrations = new Dictionary<Type, Dictionary<object, Registration>>();

        #region Basic Registrations

        /// <inheritdoc/>
        public void Register<TService, TImplementation>() where TImplementation : TService
        {
            // Default to Transient lifetime.
            Register<TService, TImplementation>(Lifetime.Transient);
        }

        /// <summary>
        /// Registers a service with its implementation type using a specified lifetime.
        /// If TService is an open generic, TImplementation must also be open generic.
        /// </summary>
        public void Register<TService, TImplementation>(Lifetime lifetime) where TImplementation : TService
        {
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);
            if (serviceType.IsGenericTypeDefinition)
            {
                // For open generic registration, implementation type must also be open.
                if (!implementationType.IsGenericTypeDefinition)
                {
                    throw new ArgumentException("Implementation type must be open generic when service type is open generic.");
                }
            }
            _registrations[serviceType] = new Registration(implementationType, lifetime);
        }

        /// <summary>
        /// Registers a service with a key.
        /// </summary>
        public void Register<TService, TImplementation>(Lifetime lifetime, object key) where TImplementation : TService
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            var serviceType = typeof(TService);
            var implementationType = typeof(TImplementation);
            if (serviceType.IsGenericTypeDefinition)
            {
                if (!implementationType.IsGenericTypeDefinition)
                {
                    throw new ArgumentException("Implementation type must be open generic when service type is open generic.", nameof(implementationType));
                }
            }
            Dictionary<object, Registration> keyedDict;
            if (!_keyedRegistrations.TryGetValue(serviceType, out keyedDict))
            {
                keyedDict = new Dictionary<object, Registration>();
                _keyedRegistrations[serviceType] = keyedDict;
            }
            keyedDict[key] = new Registration(implementationType, lifetime);
        }

        /// <summary>
        /// Registers a pre-constructed instance of a service as a singleton.
        /// Every call to <c>Resolve&lt;TService&gt;</c> will return the same instance.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="instance">The instance to register and reuse.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided instance is null.</exception>
        public void RegisterInstance<TService>(TService instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TService);
            var registration = new Registration(serviceType, Lifetime.Singleton)
            {
                SingletonInstance = instance
            };

            _registrations[serviceType] = registration;
        }

        #endregion

        #region Delegate / Factory Registration

        /// <summary>
        /// Registers a service using a factory delegate.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <param name="factory">A factory delegate that takes the container and returns an instance of TService.</param>
        /// <param name="lifetime">The lifetime of the registration.</param>
        public void Register<TService>(Func<DependencyContainer, TService> factory, Lifetime lifetime)
        {
            // We store factory registrations in the keyless dictionary.
            _registrations[typeof(TService)] = new Registration(null, lifetime)
            {
                Factory = c => factory(c)
            };
        }

        #endregion

        #region Auto-Registration (Assembly Scanning)

        /// <summary>
        /// Scans the provided assembly for types that match the predicate and registers them.
        /// For each type that is a class and satisfies the predicate, all its implemented interfaces are registered.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <param name="predicate">A predicate to filter types for registration.</param>
        public void RegisterAssemblyTypes(Assembly assembly, Func<Type, bool> predicate)
        {
            var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && predicate(t));
            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    // Register each interface mapping if not already registered.
                    if (!_registrations.ContainsKey(iface))
                    {
                        _registrations[iface] = new Registration(type, Lifetime.Transient);
                    }
                }
            }
        }

        #endregion

        #region Resolution Methods

        /// <inheritdoc/>
        public TService Resolve<TService>()
        {
            return (TService)Resolve(typeof(TService), null);
        }

        /// <summary>
        /// Resolves a service by its key.
        /// </summary>
        public TService Resolve<TService>(object key)
        {
            return (TService)Resolve(typeof(TService), null, key);
        }

        /// <summary>
        /// Resolves a service with parameter overrides.
        /// Parameter overrides are provided as a dictionary where the key is the parameter name.
        /// </summary>
        public TService Resolve<TService>(IDictionary<string, object> parameterOverrides)
        {
            return (TService)Resolve(typeof(TService), null, null, parameterOverrides);
        }

        /// <summary>
        /// Internal resolve method that supports lifetime scopes, keyed registrations, and parameter overrides.
        /// </summary>
        /// <param name="serviceType">The service type to resolve.</param>
        /// <param name="scope">The lifetime scope (if any) in which to resolve scoped services.</param>
        /// <param name="key">An optional key for keyed registrations.</param>
        /// <param name="parameterOverrides">An optional dictionary for parameter overrides.</param>
        internal object Resolve(Type serviceType, LifetimeScope scope, object key = null, IDictionary<string, object> parameterOverrides = null)
        {
            // Collection Resolver: if serviceType is IEnumerable<T>, gather all registrations for T.
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type itemType = serviceType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = (IList)Activator.CreateInstance(listType);

                // Add non-keyed registration if exists.
                try
                {
                    var instance = Resolve(itemType, scope);
                    list.Add(instance);
                }
                catch { /* Ignore if not registered. */ }

                // Add all keyed registrations if any.
                Dictionary<object, Registration> keyedDict;
                if (_keyedRegistrations.TryGetValue(itemType, out keyedDict))
                {
                    foreach (var keyVal in keyedDict.Keys)
                    {
                        var instance = Resolve(itemType, scope, keyVal);
                        list.Add(instance);
                    }
                }

                return list;
            }

            Registration registration = null;

            if (key != null)
            {
                Dictionary<object, Registration> keyedDict;
                if (_keyedRegistrations.TryGetValue(serviceType, out keyedDict))
                {
                    if (!keyedDict.TryGetValue(key, out registration))
                    {
                        throw new InvalidOperationException($"No registration found for service type '{serviceType.FullName}' with key '{key}'.");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"No keyed registrations found for service type '{serviceType.FullName}'.");
                }
            }
            else
            {
                if (!_registrations.TryGetValue(serviceType, out registration))
                {
                    // Check for open generic registration.
                    if (serviceType.IsGenericType)
                    {
                        var genericDefinition = serviceType.GetGenericTypeDefinition();
                        if (_registrations.TryGetValue(genericDefinition, out registration))
                        {
                            Type closedImplementation = registration.ImplementationType.MakeGenericType(serviceType.GetGenericArguments());
                            registration = new Registration(closedImplementation, registration.Lifetime);
                        }
                    }
                }
            }

            if (registration == null)
            {
                if (serviceType.IsClass && !serviceType.IsAbstract)
                {
                    registration = new Registration(serviceType, Lifetime.Transient);
                }
                else
                {
                    throw new InvalidOperationException($"Service type '{serviceType.FullName}' is not registered.");
                }
            }

            // Singleton lifetime: return cached instance if available.
            if (registration.Lifetime == Lifetime.Singleton && registration.SingletonInstance != null)
            {
                return registration.SingletonInstance;
            }

            // Scoped lifetime: must be resolved within a lifetime scope.
            if (registration.Lifetime == Lifetime.Scoped)
            {
                if (scope == null)
                    throw new InvalidOperationException($"Attempting to resolve scoped service '{serviceType.FullName}' outside of a lifetime scope.");
                object scopedInstance;
                if (scope.TryGetScopedInstance(registration, out scopedInstance))
                {
                    return scopedInstance;
                }
                object instanceInScope = CreateInstance(registration, scope, parameterOverrides);
                scope.AddScopedInstance(registration, instanceInScope);
                return instanceInScope;
            }

            // Transient (or Singleton if not cached yet) resolution.
            object createdInstance = CreateInstance(registration, scope, parameterOverrides);
            if (registration.Lifetime == Lifetime.Singleton)
            {
                registration.SingletonInstance = createdInstance;
            }
            return createdInstance;
        }

        #endregion

        #region Creation and Injection

        /// <summary>
        /// Creates an instance using constructor injection, applies parameter overrides if provided,
        /// and performs property injection and interception.
        /// </summary>
        private object CreateInstance(Registration registration, LifetimeScope scope, IDictionary<string, object> parameterOverrides = null)
        {
            // If a factory delegate is provided, use it.
            if (registration.Factory != null)
            {
                object factoryInstance = registration.Factory(this);
                factoryInstance = ApplyInterceptors(factoryInstance);
                return factoryInstance;
            }

            Type implementationType = registration.ImplementationType;
            ConstructorInfo constructor = implementationType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
                throw new InvalidOperationException($"No public constructors found for type '{implementationType.FullName}'.");

            // Build parameter list with overrides.
            var ctorParams = constructor.GetParameters();
            object[] parameters = ctorParams.Select(p =>
            {
                // If a parameter override exists for this parameter, use it.
                if (parameterOverrides != null && parameterOverrides.ContainsKey(p.Name))
                {
                    return parameterOverrides[p.Name];
                }
                else
                {
                    return Resolve(p.ParameterType, scope);
                }
            }).ToArray();

            object instance = Activator.CreateInstance(implementationType, parameters);
            // Apply property injection.
            InjectProperties(instance, scope);
            // Apply interception (stub).
            instance = ApplyInterceptors(instance);
            return instance;
        }

        /// <summary>
        /// Scans for properties marked with [Inject] and sets them via resolution.
        /// </summary>
        private void InjectProperties(object instance, LifetimeScope scope)
        {
            var properties = instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.IsDefined(typeof(InjectAttribute), true));
            foreach (var prop in properties)
            {
                object value = Resolve(prop.PropertyType, scope);
                prop.SetValue(instance, value);
            }
        }

        /// <summary>
        /// Applies interception to the created instance.
        /// In a real implementation, this could wrap the instance with a proxy to intercept method calls.
        /// Currently, this stub simply returns the original instance.
        /// </summary>
        private object ApplyInterceptors(object instance)
        {
            // Stub for interception. Extend using RealProxy or DispatchProxy if needed.
            return instance;
        }

        #endregion
    }
}
