using System;

namespace JsonSubTypes.Text.Json;

/// <summary>
/// Declares the decorated type as a subtype of <paramref name="baseType"/>, discovered by
/// <see cref="JsonSubtypesConverterBuilder.RegisterSubtypeAssembly"/> when the containing
/// assembly is registered. With <paramref name="discriminatorValue"/> set, the type is also
/// mapped to that discriminator value (a fully self-declaring plugin); without it, the type is
/// resolved by name like any other type in the registered assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class KnownSubTypeOfAttribute(Type baseType, object? discriminatorValue = null) : Attribute
{
    public Type BaseType { get; } = baseType;

    /// <summary>
    /// The discriminator value this subtype maps to, when the plugin declares its own mapping.
    /// <c>null</c> (the default) means the subtype is only resolved by name.
    /// </summary>
    public object? DiscriminatorValue { get; } = discriminatorValue;
}
