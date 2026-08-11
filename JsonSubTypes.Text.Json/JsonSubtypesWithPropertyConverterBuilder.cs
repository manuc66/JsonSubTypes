using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSubTypes.Text.Json
{
    [RequiresUnreferencedCode("JsonSubtypesWithPropertyConverterBuilder uses reflection to instantiate converters.")]
    [RequiresDynamicCode("JsonSubtypesWithPropertyConverterBuilder requires dynamic code to construct generic converter types.")]
    public class JsonSubtypesWithPropertyConverterBuilder
    {
        private readonly Type _baseType;
        private readonly Dictionary<string, TypeWithPropertyMatchingAttributes> _types =
            new Dictionary<string, TypeWithPropertyMatchingAttributes>();
        private Type? _fallbackType;
        private Action<UnresolvedSubtypeInfo>? _onUnresolvedSubtype;

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

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty(Type subtype, string propertyName,
            bool stopLookupOnMatch)
        {
            _types.Add(propertyName, new TypeWithPropertyMatchingAttributes(subtype, propertyName, stopLookupOnMatch));
            return this;
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty(Type subtype, string propertyName)
        {
            return RegisterSubtypeWithProperty(subtype, propertyName, false);
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

        public JsonSubtypesWithPropertyConverterBuilder OnUnresolvedSubtype(Action<UnresolvedSubtypeInfo> onUnresolvedSubtype)
        {
            _onUnresolvedSubtype = onUnresolvedSubtype;
            return this;
        }

        [RequiresUnreferencedCode("JsonSubTypes.Text.Json uses reflection to create the subtype converter.")]
        [RequiresDynamicCode("JsonSubTypes.Text.Json uses reflection to create the subtype converter.")]
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
                    typeof(bool),
                    typeof(Action<UnresolvedSubtypeInfo>)
                }, null)!;
            return (JsonConverter)constructor.Invoke(
                new object?[] { null, null, _types.Values.ToList(), _fallbackType, false, false, _onUnresolvedSubtype })!;
        }
    }
}
