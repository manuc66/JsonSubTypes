using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace JsonSubTypes.Text.Json;

/// <summary>
/// An <see cref="IJsonTypeInfoResolver"/> that configures polymorphism through the native
/// System.Text.Json contract model (<see cref="JsonPolymorphismOptions"/>) instead of a custom
/// <see cref="JsonConverter"/>. It is produced by <see cref="JsonSubtypesConverterBuilder.BuildResolver"/>
/// or <see cref="JsonSubtypesConverterBuilder.BuildResolvers"/>.
/// </summary>
/// <remarks>
/// The native polymorphism support delegates all the work to System.Text.Json. Compared to the
/// <see cref="JsonSubtypes{T}"/> converter, this resolver only supports a restricted subset of
/// the features and behaviors:
/// <list type="bullet">
/// <item>discriminator values must be <c>string</c> or <c>int</c>: <c>null</c>, <c>enum</c> and
/// other value types are not supported and throw <see cref="NotSupportedException"/> at build time;</item>
/// <item>an unrecognized type discriminator on deserialization throws by default. When
/// <see cref="JsonSubtypesConverterBuilder.IgnoreUnrecognizedTypeDiscriminators"/> is enabled it
/// falls back to the base type. A fallback to a registered subtype other than the base type is
/// not supported: <see cref="JsonSubtypesConverterBuilder.SetFallbackSubtype(Type)"/> with the
/// base type maps to the ignore-unrecognized behavior, any other value throws
/// <see cref="NotSupportedException"/> at build time;</item>
/// <item>the discriminator property is always serialized: a read-only configuration (without
/// <see cref="JsonSubtypesConverterBuilder.SerializeDiscriminatorProperty"/>) or a discriminator
/// written last (<see cref="JsonSubtypesConverterBuilder.SerializeDiscriminatorProperty(bool)"/>
/// with <c>false</c>) is refused;</item>
/// <item>the discriminator is always written first, and only for runtime types that are
/// registered as subtypes (including the base type itself when it is registered). An
/// unregistered runtime type is never silently handled: an unregistered base type is
/// serialized without a discriminator, while an unregistered derived type throws
/// <see cref="NotSupportedException"/> unless
/// <see cref="JsonSubtypesConverterBuilder.FallBackToNearestAncestor"/> is enabled, in which
/// case it is serialized as its nearest registered ancestor. With discriminator serialization
/// enabled, the <see cref="JsonSubtypes{T}"/> converter instead throws
/// <see cref="JsonException"/> for any runtime type that has no registered mapping;</item>
/// <item>only a single level of hierarchy is resolved per base type: intermediate resolvers are
/// not chained. <c>KnownSubType</c> and <c>FallBackSubType</c> attributes are honored when no
/// subtype is registered explicitly, but <c>JsonSubTypeConverter</c> is not (the resolver must
/// be built explicitly);</item>
/// <item><see cref="JsonSerializerOptions.PropertyNamingPolicy"/> is not applied to the
/// discriminator property name, and case-insensitive matching of that name is not supported;</item>
/// <item>the discriminator property name must not equal the serialized name of any property:
/// on .NET 8 this silently produces a JSON object with duplicate keys, and on .NET 10 it throws
/// <see cref="InvalidOperationException"/>;</item>
/// <item>a missing discriminator on an interface or abstract base type throws
/// <see cref="NotSupportedException"/> instead of <see cref="JsonException"/>.</item>
/// </list>
/// Use the <see cref="JsonSubtypes{T}"/> converter (via
/// <see cref="JsonSubtypesConverterBuilder.Build"/>) when the configuration does not fit these
/// constraints.
/// <para>
/// Do not combine several instances through <see cref="JsonSerializerOptions.TypeInfoResolverChain"/>:
/// each resolver answers for every type, so only the first one would be applied. Use
/// <see cref="JsonSubtypesConverterBuilder.BuildResolvers"/> to handle several
/// hierarchies from a single resolver instead.
/// </para>
/// <para>
/// The resolver delegates to a shared reflection-based
/// <c>DefaultJsonTypeInfoResolver</c> instance. Metadata customization applied to a
/// different <c>DefaultJsonTypeInfoResolver</c> instance (for example through
/// <c>Modifiers</c>) does not apply here.
/// </para>
/// </remarks>
[RequiresUnreferencedCode("JsonSubtypesResolver uses reflection to resolve type metadata.")]
[RequiresDynamicCode("JsonSubtypesResolver requires dynamic code to construct type metadata.")]
public sealed class JsonSubtypesResolver : IJsonTypeInfoResolver
{
    private static readonly IJsonTypeInfoResolver InnerResolver = new DefaultJsonTypeInfoResolver();

    private readonly Dictionary<Type, JsonPolymorphismOptions> _polymorphismOptions;

    internal JsonSubtypesResolver(IReadOnlyList<BaseTypeRegistration> registrations)
    {
        _polymorphismOptions = new Dictionary<Type, JsonPolymorphismOptions>();
        foreach (BaseTypeRegistration registration in registrations)
        {
            JsonPolymorphismOptions options = new()
            {
                TypeDiscriminatorPropertyName = registration.DiscriminatorPropertyName,
                UnknownDerivedTypeHandling = registration.UnknownDerivedTypeHandling,
                IgnoreUnrecognizedTypeDiscriminators = registration.IgnoreUnrecognizedTypeDiscriminators
            };
            foreach (JsonDerivedType derivedType in registration.DerivedTypes)
            {
                options.DerivedTypes.Add(derivedType);
            }

            _polymorphismOptions[registration.BaseType] = options;
        }
    }

    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo? info = InnerResolver.GetTypeInfo(type, options);
        if (info != null && _polymorphismOptions.TryGetValue(info.Type, out JsonPolymorphismOptions? polymorphismOptions))
        {
            if (info.PolymorphismOptions != null)
            {
                throw new InvalidOperationException(
                    $"Type {info.Type.FullName} already declares polymorphism through [JsonPolymorphic]/[JsonDerivedType] attributes. Remove those attributes or do not use the JsonSubtypesResolver for this type.");
            }

            info.PolymorphismOptions = polymorphismOptions;
        }

        return info;
    }

    internal readonly struct BaseTypeRegistration
    {
        public BaseTypeRegistration(Type baseType, string discriminatorPropertyName,
            List<JsonDerivedType> derivedTypes, JsonUnknownDerivedTypeHandling unknownDerivedTypeHandling,
            bool ignoreUnrecognizedTypeDiscriminators)
        {
            BaseType = baseType;
            DiscriminatorPropertyName = discriminatorPropertyName;
            DerivedTypes = derivedTypes;
            UnknownDerivedTypeHandling = unknownDerivedTypeHandling;
            IgnoreUnrecognizedTypeDiscriminators = ignoreUnrecognizedTypeDiscriminators;
        }

        public Type BaseType { get; }
        public string DiscriminatorPropertyName { get; }
        public List<JsonDerivedType> DerivedTypes { get; }
        public JsonUnknownDerivedTypeHandling UnknownDerivedTypeHandling { get; }
        public bool IgnoreUnrecognizedTypeDiscriminators { get; }
    }
}
