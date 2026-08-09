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

        public JsonConverter Build()
        {
            Type converterType = typeof(JsonSubtypes<>).MakeGenericType(_baseType);
            ConstructorInfo constructor = converterType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new[]
                {
                    typeof(string),
                    typeof(NullableDictionary<object, Type>),
                    typeof(List<TypeWithPropertyMatchingAttributes>),
                    typeof(Type)
                }, null)!;
            return (JsonConverter)constructor.Invoke(
                new object?[] { _discriminatorProperty, _subTypeMapping, null, _fallbackType })!;
        }
    }
}
