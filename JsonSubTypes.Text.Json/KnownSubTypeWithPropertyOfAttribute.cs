using System;

namespace JsonSubTypes.Text.Json;

/// <summary>
/// Declares the decorated type as a subtype of <paramref name="baseType"/>, identified by the
/// presence of <paramref name="propertyName"/> in the JSON. Discovered by
/// <see cref="JsonSubtypesWithPropertyConverterBuilder.RegisterSubtypeAssembly"/> when the
/// containing assembly is registered — the self-declaring plugin pattern for property-presence
/// discrimination. Mirrors <see cref="KnownSubTypeWithPropertyAttribute"/> from the subtype side.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class KnownSubTypeWithPropertyOfAttribute(Type baseType, string propertyName) : Attribute
{
    public Type BaseType { get; } = baseType;
    public string PropertyName { get; } = propertyName;
}
