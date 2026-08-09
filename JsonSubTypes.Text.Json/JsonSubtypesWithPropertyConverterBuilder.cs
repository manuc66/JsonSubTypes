using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSubTypes.Text.Json
{
    public class JsonSubtypesWithPropertyConverterBuilder
    {
        private readonly Type _baseType;
        private readonly List<TypeWithPropertyMatchingAttributes> _types =
            new List<TypeWithPropertyMatchingAttributes>();
        private Type? _fallbackType;

        private JsonSubtypesWithPropertyConverterBuilder(Type baseType)
        {
            _baseType = baseType;
        }

        public static JsonSubtypesWithPropertyConverterBuilder Of(Type baseType)
        {
            return new JsonSubtypesWithPropertyConverterBuilder(baseType);
        }

        public static JsonSubtypesWithPropertyConverterBuilder Of<T>()
        {
            return new JsonSubtypesWithPropertyConverterBuilder(typeof(T));
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty(Type subtype, string propertyName)
        {
            _types.Add(new TypeWithPropertyMatchingAttributes(subtype, propertyName, false));
            return this;
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty<T>(string propertyName)
        {
            return RegisterSubtypeWithProperty(typeof(T), propertyName);
        }

        public JsonSubtypesWithPropertyConverterBuilder SetFallbackSubtype(Type fallbackSubtype)
        {
            _fallbackType = fallbackSubtype;
            return this;
        }

        public JsonSubtypesWithPropertyConverterBuilder SetFallbackSubtype<T>()
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
                    typeof(Type),
                    typeof(bool),
                    typeof(bool)
                }, null)!;
            return (JsonConverter)constructor.Invoke(
                new object?[] { null, null, _types, _fallbackType, false, false })!;
        }
    }
}
