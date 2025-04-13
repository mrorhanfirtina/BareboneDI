using System;

namespace BareboneDI.Attributes
{
    /// <summary>
    /// Indicates that a property should have its dependency injected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class InjectAttribute : Attribute
    {
    }
}
