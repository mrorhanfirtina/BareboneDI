namespace BareboneDI.Interfaces
{
    /// <summary>
    /// Defines the basic contract for a dependency injection container.
    /// Allows registering and resolving service dependencies.
    /// </summary>
    public interface IDependencyContainer
    {
        /// <summary>
        /// Registers a service type and its concrete implementation type.
        /// </summary>
        /// <typeparam name="TService">The service interface or base type.</typeparam>
        /// <typeparam name="TImplementation">The concrete implementation type.</typeparam>
        void Register<TService, TImplementation>() where TImplementation : TService;

        /// <summary>
        /// Resolves an instance of the specified service type.
        /// </summary>
        /// <typeparam name="TService">The type of service to resolve.</typeparam>
        /// <returns>An instance of the specified service type.</returns>
        TService Resolve<TService>();

        // Overloads for keyed registrations
        void Register<TService, TImplementation>(Enums.Lifetime lifetime, object key) where TImplementation : TService;
        TService Resolve<TService>(object key);
    }
}
