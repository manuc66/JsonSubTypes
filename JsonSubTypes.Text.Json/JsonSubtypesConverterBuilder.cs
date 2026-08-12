using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace JsonSubTypes.Text.Json
{
    [RequiresUnreferencedCode("JsonSubtypesConverterBuilder uses reflection to instantiate converters.")]
    [RequiresDynamicCode("JsonSubtypesConverterBuilder requires dynamic code to construct generic converter types.")]
    public class JsonSubtypesConverterBuilder
    {
        private readonly Type _baseType;
        private readonly string _discriminatorProperty;
        private readonly NullableDictionary<object, Type> _subTypeMapping = new NullableDictionary<object, Type>();
        private Type? _fallbackType;
        private bool _serializeDiscriminatorProperty;
        private bool _addDiscriminatorFirst;
        private bool _ignoreUnrecognizedTypeDiscriminators;
        private bool _fallBackToNearestAncestor;

        private JsonSubtypesConverterBuilder(Type baseType, string discriminatorProperty)
        {
            _baseType = baseType;
            _discriminatorProperty = discriminatorProperty;
        }

        public static JsonSubtypesConverterBuilder Of(Type baseType, string discriminatorProperty)
        {
            return new JsonSubtypesConverterBuilder(baseType, discriminatorProperty);
        }

        public static JsonSubtypesConverterBuilder Of<T>(string discriminatorProperty)
        {
            return new JsonSubtypesConverterBuilder(typeof(T), discriminatorProperty);
        }

        public JsonSubtypesConverterBuilder RegisterSubtype(Type subtype, object? value)
        {
            _subTypeMapping.Add(value, subtype);
            return this;
        }

        public JsonSubtypesConverterBuilder RegisterSubtype<T>(object? value)
        {
            return RegisterSubtype(typeof(T), value);
        }

        public JsonSubtypesConverterBuilder SetFallbackSubtype(Type fallbackSubtype)
        {
            _fallbackType = fallbackSubtype;
            return this;
        }

        public JsonSubtypesConverterBuilder SetFallbackSubtype<T>()
        {
            return SetFallbackSubtype(typeof(T));
        }

        public JsonSubtypesConverterBuilder SerializeDiscriminatorProperty()
        {
            return SerializeDiscriminatorProperty(true);
        }

        public JsonSubtypesConverterBuilder SerializeDiscriminatorProperty(bool addDiscriminatorFirst)
        {
            _serializeDiscriminatorProperty = true;
            _addDiscriminatorFirst = addDiscriminatorFirst;
            return this;
        }

        /// <summary>
        /// Makes the native resolver (<see cref="BuildResolver"/>) fall back to the base type when a
        /// type discriminator is not recognized during deserialization, instead of throwing.
        /// Only supported by <see cref="BuildResolver"/>; <see cref="Build"/> throws if this is set.
        /// </summary>
        public JsonSubtypesConverterBuilder IgnoreUnrecognizedTypeDiscriminators()
        {
            _ignoreUnrecognizedTypeDiscriminators = true;
            return this;
        }

        /// <summary>
        /// Makes the native resolver (<see cref="BuildResolver"/>) serialize an unregistered derived
        /// type as its nearest registered ancestor instead of throwing, when
        /// <c>System.Text.Json</c> supports it (.NET 8 and later). Only supported by
        /// <see cref="BuildResolver"/>; <see cref="Build"/> throws if this is set.
        /// </summary>
        public JsonSubtypesConverterBuilder FallBackToNearestAncestor()
        {
            _fallBackToNearestAncestor = true;
            return this;
        }

        [RequiresUnreferencedCode("JsonSubTypes.Text.Json uses reflection to create the subtype converter.")]
        [RequiresDynamicCode("JsonSubTypes.Text.Json uses reflection to create the subtype converter.")]
        public JsonConverter Build()
        {
            if (_ignoreUnrecognizedTypeDiscriminators || _fallBackToNearestAncestor)
            {
                throw new NotSupportedException(
                    "IgnoreUnrecognizedTypeDiscriminators and FallBackToNearestAncestor are only supported by BuildResolver(). Use Build() to obtain the JsonSubtypes converter without these options.");
            }

            if (_serializeDiscriminatorProperty)
            {
                HashSet<Type> seenTypes = new HashSet<Type>();
                foreach (KeyValuePair<object?, Type> entry in _subTypeMapping.Entries())
                {
                    if (!seenTypes.Add(entry.Value))
                    {
                        throw new InvalidOperationException(
                            "Multiple discriminators on single type are not supported when discriminator serialization is enabled");
                    }
                }
            }

            Type converterType = typeof(JsonSubtypes<>).MakeGenericType(_baseType);
            ConstructorInfo constructor = converterType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[]
                {
                    typeof(string),
                    typeof(NullableDictionary<object, Type>),
                    typeof(List<TypeWithPropertyMatchingAttributes>),
                    typeof(Type),
                    typeof(bool),
                    typeof(bool)
                }, null)!;
            return (JsonConverter)constructor.Invoke(
                new object?[]
                {
                    _discriminatorProperty, _subTypeMapping, null, _fallbackType,
                    _serializeDiscriminatorProperty, _addDiscriminatorFirst
                })!;
        }

        /// <summary>
        /// Builds a <see cref="JsonSubtypesResolver"/> that delegates polymorphism to the native
        /// System.Text.Json contract model instead of using a custom <see cref="JsonConverter"/>.
        /// Assign the result to <see cref="JsonSerializerOptions.TypeInfoResolver"/>.
        /// </summary>
        /// <remarks>
        /// Only a subset of the converter configuration is supported:
        /// <list type="bullet">
        /// <item>discriminator values must be <c>string</c> or <c>int</c> (no <c>null</c>, no enum);</item>
        /// <item>falling back to the base type is supported, either with
        /// <see cref="SetFallbackSubtype(Type)"/> when the fallback is the base type or with
        /// <see cref="IgnoreUnrecognizedTypeDiscriminators"/>. A fallback to a subtype other than
        /// the base type is not supported;</item>
        /// <item>when no subtype is registered explicitly, <c>KnownSubType</c> and
        /// <c>FallBackSubType</c> attributes on the base type are honored;</item>
        /// <item><see cref="SerializeDiscriminatorProperty()"/> must have been called and the
        /// discriminator must be written first (the native resolver always writes the discriminator
        /// property, always first, and only for runtime types that are registered as subtypes,
        /// including the base type when it is registered). An unregistered derived type can be
        /// serialized as its nearest registered ancestor with
        /// <see cref="FallBackToNearestAncestor"/>;</item>
        /// <item>only a single level of hierarchy is resolved per base type. To handle several base
        /// type hierarchies, combine builders with <see cref="BuildResolvers"/>. Combining
        /// resolvers through <see cref="JsonSerializerOptions.TypeInfoResolverChain"/> does not work,
        /// because each resolver answers for every type and only the first one would be applied;</item>
        /// <item>the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> is not applied to the
        /// discriminator property name, and case-insensitive matching of that name is not supported.</item>
        /// </list>
        /// Use <see cref="Build"/> instead when the configuration does not fit these constraints.
        /// </remarks>
        [RequiresUnreferencedCode("JsonSubTypes.Text.Json uses reflection to resolve type metadata.")]
        [RequiresDynamicCode("JsonSubTypes.Text.Json uses reflection to construct type metadata.")]
        public JsonSubtypesResolver BuildResolver()
        {
            return new JsonSubtypesResolver(
                new[] { BuildRegistration() });
        }

        /// <summary>
        /// Builds a <see cref="JsonSubtypesResolver"/> that configures polymorphism for all the
        /// given builders through the native System.Text.Json contract model. This is the only way
        /// to handle several base type hierarchies from the native resolver: combining several
        /// resolvers through <see cref="JsonSerializerOptions.TypeInfoResolverChain"/> does not work,
        /// because each resolver answers for every type and only the first one would be applied.
        /// </summary>
        /// <param name="builders">The builders to combine. Each must be valid for
        /// <see cref="BuildResolver"/>.</param>
        /// <returns>A <see cref="JsonSubtypesResolver"/> handling all the given hierarchies.</returns>
        [RequiresUnreferencedCode("JsonSubTypes.Text.Json uses reflection to resolve type metadata.")]
        [RequiresDynamicCode("JsonSubTypes.Text.Json uses reflection to construct type metadata.")]
        public static JsonSubtypesResolver BuildResolvers(
            params JsonSubtypesConverterBuilder[] builders)
        {
            if (builders.Length == 0)
            {
                throw new ArgumentException("At least one builder is required.", nameof(builders));
            }

            HashSet<Type> seenBaseTypes = new HashSet<Type>();
            List<JsonSubtypesResolver.BaseTypeRegistration> registrations =
                new List<JsonSubtypesResolver.BaseTypeRegistration>();
            foreach (JsonSubtypesConverterBuilder builder in builders)
            {
                if (!seenBaseTypes.Add(builder._baseType))
                {
                    throw new ArgumentException(
                        $"Several builders target the same base type {builder._baseType.FullName}. Combine them in a single builder instead.",
                        nameof(builders));
                }

                registrations.Add(builder.BuildRegistration());
            }

            return new JsonSubtypesResolver(registrations);
        }

        private JsonSubtypesResolver.BaseTypeRegistration BuildRegistration()
        {
            NullableDictionary<object, Type> subTypeMapping = _subTypeMapping.Entries().Any()
                ? _subTypeMapping
                : BuildAttributeSubTypeMapping(_baseType);

            if (!subTypeMapping.Entries().Any())
            {
                throw new InvalidOperationException(
                    "Cannot build a type info resolver without any registered subtype. Call RegisterSubtype before building, or apply KnownSubType attributes to the base type.");
            }

            Type? fallbackType = _fallbackType ?? GetFallbackSubTypeAttribute(_baseType)?.SubType;
            bool ignoreUnrecognizedTypeDiscriminators = _ignoreUnrecognizedTypeDiscriminators;
            if (fallbackType != null)
            {
                if (fallbackType == _baseType)
                {
                    ignoreUnrecognizedTypeDiscriminators = true;
                }
                else
                {
                    throw new NotSupportedException(
                        "SetFallbackSubtype is not supported by the native type info resolver when the fallback is not the base type. Use Build() to obtain the JsonSubtypes converter instead.");
                }
            }

            if (!_serializeDiscriminatorProperty)
            {
                throw new NotSupportedException(
                    "The native type info resolver always serializes the discriminator property. Call SerializeDiscriminatorProperty() to opt in, or use Build() to obtain a read-only converter.");
            }

            if (!_addDiscriminatorFirst)
            {
                throw new NotSupportedException(
                    "The native type info resolver always writes the discriminator property first. Use Build() with SerializeDiscriminatorProperty(false) instead.");
            }

            HashSet<Type> seenTypes = new HashSet<Type>();
            List<JsonDerivedType> derivedTypes = new List<JsonDerivedType>();
            foreach (KeyValuePair<object?, Type> entry in subTypeMapping.Entries())
            {
                if (!seenTypes.Add(entry.Value))
                {
                    throw new InvalidOperationException(
                        "Multiple discriminators on single type are not supported by the native type info resolver. Use Build() to obtain the JsonSubtypes converter instead.");
                }

                derivedTypes.Add(CreateJsonDerivedType(entry.Value, entry.Key));
            }

            JsonUnknownDerivedTypeHandling unknownDerivedTypeHandling = _fallBackToNearestAncestor
                ? JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                : JsonUnknownDerivedTypeHandling.FailSerialization;

            return new JsonSubtypesResolver.BaseTypeRegistration(
                _baseType, _discriminatorProperty, derivedTypes, unknownDerivedTypeHandling,
                ignoreUnrecognizedTypeDiscriminators);
        }

        private static NullableDictionary<object, Type> BuildAttributeSubTypeMapping(Type type)
        {
            NullableDictionary<object, Type> dictionary = new NullableDictionary<object, Type>();
            foreach (object attribute in type.GetTypeInfo().GetCustomAttributes(false))
            {
                if (attribute is KnownSubTypeAttribute known)
                {
                    dictionary.Add(known.AssociatedValue, known.SubType);
                }
            }

            return dictionary;
        }

        private static FallBackSubTypeAttribute? GetFallbackSubTypeAttribute(Type type)
        {
            foreach (object attribute in type.GetTypeInfo().GetCustomAttributes(false))
            {
                if (attribute is FallBackSubTypeAttribute fallback)
                {
                    return fallback;
                }
            }

            return null;
        }

        private static JsonDerivedType CreateJsonDerivedType(Type subtype, object? discriminator)
        {
            switch (discriminator)
            {
                case null:
                    throw new NotSupportedException(
                        "A null discriminator value is not supported by the native type info resolver. Use Build() to obtain the JsonSubtypes converter instead.");
                case string stringDiscriminator:
                    return new JsonDerivedType(subtype, stringDiscriminator);
                case int intDiscriminator:
                    return new JsonDerivedType(subtype, intDiscriminator);
                default:
                    throw new NotSupportedException(
                        $"Discriminator values of type {discriminator.GetType().Name} are not supported by the native type info resolver; only string and int are supported. Use Build() to obtain the JsonSubtypes converter instead.");
            }
        }
    }
}
