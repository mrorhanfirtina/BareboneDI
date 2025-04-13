namespace BareboneDI.Enums
{
    /// <summary>
    /// Specifies the lifetime of a registration.
    /// </summary>
    public enum Lifetime
    {
        /// <summary>
        /// Every resolve call creates a new instance.
        /// </summary>
        Transient,

        /// <summary>
        /// The same instance is returned for every resolve call.
        /// </summary>
        Singleton,

        /// <summary>
        /// The same instance is returned within a specific lifetime scope.
        /// </summary>
        Scoped
    }
}
