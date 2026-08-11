using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace JsonSubTypes.Text.Json
{
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
        /// <item>no fallback subtype is supported (<see cref="JsonSubtypesConverterBuilder.SetFallbackSubtype(Type)"/>
        /// throws <see cref="NotSupportedException"/>);</item>
    /// <item>the discriminator property is always serialized: a read-only configuration (without
    /// <see cref="JsonSubtypesConverterBuilder.SerializeDiscriminatorProperty"/>) or a discriminator
    /// written last (<see cref="JsonSubtypesConverterBuilder.SerializeDiscriminatorProperty(bool)"/>
    /// with <c>false</c>) is refused;</item>
    /// <item>the discriminator is always written first, and only for runtime types that are
    /// registered as subtypes (including the base type itself when it is registered). An
    /// unregistered runtime type is never silently handled: an unregistered base type is
    /// serialized without a discriminator, while an unregistered derived type throws
    /// <see cref="NotSupportedException"/>. With discriminator serialization enabled, the
    /// <see cref="JsonSubtypes{T}"/> converter instead throws <see cref="JsonException"/> for any
    /// runtime type that has no registered mapping;</item>
    /// <item>only a single level of hierarchy is resolved per base type: intermediate resolvers are
    /// not chained, and attribute-based configuration (<c>KnownSubType</c>, <c>FallBackSubType</c>,
    /// <c>JsonSubTypeConverter</c>) is not supported;</item>
    /// <item><see cref="JsonSerializerOptions.PropertyNamingPolicy"/> is not applied to the
    /// discriminator property name, and case-insensitive matching of that name is not supported;</item>
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
    /// </remarks>
    [RequiresUnreferencedCode("JsonSubtypesResolver uses reflection to resolve type metadata.")]
    [RequiresDynamicCode("JsonSubtypesResolver requires dynamic code to construct type metadata.")]
    public sealed class JsonSubtypesResolver : IJsonTypeInfoResolver
    {
        private static readonly IJsonTypeInfoResolver InnerResolver = new DefaultJsonTypeInfoResolver();

        private readonly IReadOnlyList<BaseTypeRegistration> _registrations;

        internal JsonSubtypesResolver(IReadOnlyList<BaseTypeRegistration> registrations)
        {
            _registrations = registrations;
        }

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo? info = InnerResolver.GetTypeInfo(type, options);
            if (info != null)
            {
                foreach (BaseTypeRegistration registration in _registrations)
                {
                    if (registration.BaseType == info.Type)
                    {
                        info.PolymorphismOptions = new JsonPolymorphismOptions
                        {
                            TypeDiscriminatorPropertyName = registration.DiscriminatorPropertyName
                        };
                        foreach (JsonDerivedType derivedType in registration.DerivedTypes)
                        {
                            info.PolymorphismOptions.DerivedTypes.Add(derivedType);
                        }

                        break;
                    }
                }
            }

            return info;
        }

        internal readonly struct BaseTypeRegistration
        {
            public BaseTypeRegistration(Type baseType, string discriminatorPropertyName,
                List<JsonDerivedType> derivedTypes)
            {
                BaseType = baseType;
                DiscriminatorPropertyName = discriminatorPropertyName;
                DerivedTypes = derivedTypes;
            }

            public Type BaseType { get; }
            public string DiscriminatorPropertyName { get; }
            public List<JsonDerivedType> DerivedTypes { get; }
        }
    }
}
