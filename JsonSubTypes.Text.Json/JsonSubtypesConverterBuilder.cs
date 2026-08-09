using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSubTypes.Text.Json
{
    public class JsonSubtypesConverterBuilder
    {
        private readonly Type _baseType;
        private readonly string _discriminatorProperty;
        private readonly NullableDictionary<object, Type> _subTypeMapping = new NullableDictionary<object, Type>();
        private Type? _fallbackType;
        private bool _serializeDiscriminatorProperty;
        private bool _addDiscriminatorFirst;

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

        public JsonConverter Build()
        {
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
    }
}
