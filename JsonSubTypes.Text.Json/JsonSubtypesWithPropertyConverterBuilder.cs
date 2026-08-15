using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace JsonSubTypes.Text.Json;

[RequiresUnreferencedCode("JsonSubtypesWithPropertyConverterBuilder uses reflection to instantiate converters.")]
[RequiresDynamicCode("JsonSubtypesWithPropertyConverterBuilder requires dynamic code to construct generic converter types.")]
public class JsonSubtypesWithPropertyConverterBuilder
{
    private readonly Type _baseType;
    private readonly Dictionary<string, TypeWithPropertyMatchingAttributes> _types = new();
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

    [RequiresUnreferencedCode("JsonSubTypes.Text.Json uses reflection to create the subtype converter.")]
    [RequiresDynamicCode("JsonSubTypes.Text.Json uses reflection to create the subtype converter.")]
    public JsonConverter Build()
    {
        Type converterType = typeof(JsonSubtypes<>).MakeGenericType(_baseType);
        ConstructorInfo constructor = converterType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            [
                typeof(string),
                typeof(NullableDictionary<object, Type>),
                typeof(List<TypeWithPropertyMatchingAttributes>),
                typeof(Type),
                typeof(bool),
                typeof(bool),
                typeof(Assembly[])
            ], null)!;
        return (JsonConverter)constructor.Invoke(
            [null, null, _types.Values.ToList(), _fallbackType, false, false, Array.Empty<Assembly>()]);
    }
}
