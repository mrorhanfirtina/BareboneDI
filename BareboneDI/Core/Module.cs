namespace BareboneDI.Core
{
    /// <summary>
    /// Abstract base class for modules that group related service registrations.
    /// </summary>
    public abstract class Module
    {
        /// <summary>
        /// Loads registrations into the container.
        /// </summary>
        /// <param name="container">The container to load registrations into.</param>
        public abstract void Load(DependencyContainer container);
    }
}
