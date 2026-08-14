using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSubTypes.Text.Json;

[RequiresUnreferencedCode("JsonSubTypeConverterAttribute uses reflection to instantiate converters.")]
[RequiresDynamicCode("JsonSubTypeConverterAttribute requires dynamic code to instantiate converters.")]
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum |
    AttributeTargets.Property, AllowMultiple = false)]
public class JsonSubTypeConverterAttribute : JsonConverterAttribute
{
    public string? DiscriminatorPropertyName { get; }

    public JsonSubTypeConverterAttribute(Type converterType, string? discriminatorPropertyName) : base(converterType)
    {
        DiscriminatorPropertyName = discriminatorPropertyName;
    }

    public JsonSubTypeConverterAttribute(Type converterType) : base(converterType)
    {
    }

    [RequiresUnreferencedCode("JsonSubTypes.Text.Json uses reflection to create and invoke subtype converters.")]
    [RequiresDynamicCode("JsonSubTypes.Text.Json uses reflection to create subtype converters.")]
    public override JsonConverter CreateConverter(Type typeToConvert)
    {
        return DiscriminatorPropertyName == null
            ? (JsonConverter)Activator.CreateInstance(ConverterType!)!
            : (JsonConverter)Activator.CreateInstance(ConverterType!, DiscriminatorPropertyName)!;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class KnownSubTypeAttribute(Type subType, object? associatedValue) : Attribute
{
    public Type SubType { get; } = subType;
    public object? AssociatedValue { get; } = associatedValue;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class FallBackSubTypeAttribute(Type subType) : Attribute
{
    public Type SubType { get; } = subType;
}

/// <summary>
/// Opts a base type into the JsonSubTypes.Text.Json.Aot source generator, which emits a compiled
/// <see cref="JsonConverter{T}"/> that routes subtypes without reflection (Native AOT friendly).
/// Unlike <see cref="JsonSubTypeConverterAttribute"/>, this attribute is not a
/// <see cref="JsonConverterAttribute"/> and does not interfere with the System.Text.Json source
/// generator. The discriminator property name is required for value-based discrimination; omit
/// it to use property-presence discrimination (<see cref="KnownSubTypeWithPropertyAttribute"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public class JsonSubTypesAotConverterAttribute : Attribute
{
    public string? DiscriminatorPropertyName { get; }

    /// <summary>Whether the discriminator is written first. Defaults to <c>true</c>.</summary>
    public bool AddDiscriminatorFirst { get; set; } = true;

    public JsonSubTypesAotConverterAttribute()
    {
    }

    public JsonSubTypesAotConverterAttribute(string discriminatorPropertyName)
    {
        DiscriminatorPropertyName = discriminatorPropertyName;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class KnownSubTypeWithPropertyAttribute(Type subType, string propertyName) : Attribute
{
    public Type SubType { get; } = subType;
    public string PropertyName { get; } = propertyName;
    public bool StopLookupOnMatch { get; set; }
}

internal interface IJsonSubtypes
{
    Type GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions jsonSerializerOptions);
    bool CanConvert(Type toType);
}

[RequiresUnreferencedCode("JsonSubtypes uses reflection to discover sub-types and properties.")]
[RequiresDynamicCode("JsonSubtypes requires dynamic code for runtime type creation.")]
/// <summary>
/// A JSON converter that deserializes a polymorphic hierarchy from a discriminator property.
/// The concrete subtype is resolved from an explicit mapping (<see cref="KnownSubTypeAttribute"/>)
/// or, when no mapping is declared, by matching the discriminator string against a type name.
/// </summary>
/// <remarks>
/// <para>
/// Name-based resolution (used only when no <see cref="KnownSubTypeAttribute"/> mapping is
/// declared) instantiates the type whose name matches the discriminator, provided it is
/// assignable from the polymorphic base type and lives in the base type's assembly or in an
/// assembly registered via <see cref="JsonSubTypesTypeResolution"/>. Any such type present in
/// those assemblies can be instantiated with attacker-controlled JSON.
/// </para>
/// <para>
/// Do not expose a name-based hierarchy to untrusted JSON without validating the payload
/// upstream; prefer an explicit <see cref="KnownSubTypeAttribute"/> mapping whenever the
/// discriminator can come from outside your own code.
/// </para>
/// </remarks>
public class JsonSubtypes<T> : JsonConverter<T>, IJsonSubtypes where T : class
{
    private static readonly ConcurrentDictionary<Type, Action<Utf8JsonWriter, object, JsonSerializerOptions>>
        BaseTypeWriterCache = new();

    private static readonly ConcurrentDictionary<Type, Action<object, JsonElement, JsonSerializerOptions>>
        BaseTypeObjectReaderCache = new();

    private static readonly ConcurrentDictionary<Type, Dictionary<Type, object?>?>
        AttributeReverseMapCache = new();

    private static readonly ConcurrentDictionary<Type, JsonSubTypeConverterAttribute?>
        ConverterAttributeCache = new();

    private static readonly ConcurrentDictionary<(Type OuterType, Type TargetType), IJsonSubtypes>
        AttributeResolverCache = new();

    // The converter resolution walk scans serializer.Converters for IJsonSubtypes
    // instances. The result is stable for the lifetime of the options (System.Text.Json
    // freezes JsonSerializerOptions on first use), so cache it instead of re-scanning
    // and re-allocating a list on every deserialized object.
    private static readonly ConditionalWeakTable<JsonSerializerOptions, IJsonSubtypes[]>
        OptionsConverterCache = new();

    protected readonly string? JsonDiscriminatorPropertyName;

    private readonly NullableDictionary<object, Type>? _subTypeMapping;
    private readonly List<TypeWithPropertyMatchingAttributes>? _typesByPropertyPresence;
    private readonly Type? _fallbackType;
    private readonly bool _serializeDiscriminatorProperty;
    private readonly bool _addDiscriminatorFirst;
    private readonly Dictionary<Type, object?>? _runtimeTypeToDiscriminator;

    public JsonSubtypes()
    {
    }

    public JsonSubtypes(string? jsonDiscriminatorPropertyName)
    {
        JsonDiscriminatorPropertyName = jsonDiscriminatorPropertyName;
        _serializeDiscriminatorProperty = jsonDiscriminatorPropertyName != null;
        _addDiscriminatorFirst = true;
    }

    internal JsonSubtypes(string? jsonDiscriminatorPropertyName,
        NullableDictionary<object, Type>? subTypeMapping,
        List<TypeWithPropertyMatchingAttributes>? typesByPropertyPresence,
        Type? fallbackType,
        bool serializeDiscriminatorProperty,
        bool addDiscriminatorFirst) : this(jsonDiscriminatorPropertyName)
    {
        _subTypeMapping = subTypeMapping;
        _typesByPropertyPresence = typesByPropertyPresence;
        _fallbackType = fallbackType;
        _serializeDiscriminatorProperty = serializeDiscriminatorProperty;
        _addDiscriminatorFirst = addDiscriminatorFirst;
        if (subTypeMapping != null)
        {
            _runtimeTypeToDiscriminator = new Dictionary<Type, object?>();
            foreach (KeyValuePair<object?, Type> entry in subTypeMapping.Entries())
            {
                _runtimeTypeToDiscriminator[entry.Value] = entry.Key;
            }
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(T);
    }

    public override T? Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
    {
        return ReadJson(ref reader, objectType, serializer);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions serializer)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        Type runtimeType = value.GetType();

        if (JsonDiscriminatorPropertyName == null)
        {
            WritePlain(writer, value, runtimeType, serializer);
            return;
        }

        if (runtimeType != typeof(T))
        {
            if (_serializeDiscriminatorProperty && TryGetDiscriminatorValue(runtimeType, out object? discriminatorValue))
            {
                string json = JsonSerializer.Serialize(value, runtimeType, serializer);
                WriteObjectWithDiscriminator(writer, json, discriminatorValue, serializer);
                return;
            }

            if (_serializeDiscriminatorProperty && _subTypeMapping != null)
            {
                ThrowImpossibleToSerialize(runtimeType);
            }

            JsonSerializer.Serialize<object>(writer, value, serializer);
            return;
        }

        if (_serializeDiscriminatorProperty && TryGetDiscriminatorValue(runtimeType, out object? baseDiscriminatorValue))
        {
            Action<Utf8JsonWriter, object, JsonSerializerOptions> baseWriter =
                BaseTypeWriterCache.GetOrAdd(typeof(T), static type => BuildBaseTypeWriter(type));
            using MemoryStream stream = new();
            using (Utf8JsonWriter bufferWriter = new(stream))
            {
                baseWriter(bufferWriter, value, serializer);
            }
            WriteObjectWithDiscriminator(writer, Encoding.UTF8.GetString(stream.ToArray()), baseDiscriminatorValue,
                serializer);
            return;
        }

        if (_serializeDiscriminatorProperty && _subTypeMapping != null)
        {
            ThrowImpossibleToSerialize(runtimeType);
        }

        WritePlain(writer, value, runtimeType, serializer);
    }

    private static void WritePlain(Utf8JsonWriter writer, T value, Type runtimeType, JsonSerializerOptions serializer)
    {
        if (runtimeType != typeof(T))
        {
            JsonSerializer.Serialize<object>(writer, value, serializer);
            return;
        }

        Action<Utf8JsonWriter, object, JsonSerializerOptions> baseTypeWriter =
            BaseTypeWriterCache.GetOrAdd(typeof(T), static type => BuildBaseTypeWriter(type));
        baseTypeWriter(writer, value, serializer);
    }

    private bool TryGetDiscriminatorValue(Type runtimeType, out object? discriminatorValue)
    {
        if (_runtimeTypeToDiscriminator != null && _runtimeTypeToDiscriminator.TryGetValue(runtimeType, out discriminatorValue))
        {
            return true;
        }

        Dictionary<Type, object?>? attributeReverseMap =
            AttributeReverseMapCache.GetOrAdd(typeof(T), static type => BuildAttributeReverseMap(type));
        if (attributeReverseMap != null && attributeReverseMap.TryGetValue(runtimeType, out discriminatorValue))
        {
            return true;
        }

        discriminatorValue = null;
        return false;
    }

    private static void ThrowImpossibleToSerialize(Type runtimeType)
    {
        throw new JsonException(
            $"Impossible to serialize type: {runtimeType.FullName} because there is no registered mapping for the discriminator property");
    }

    private void WriteObjectWithDiscriminator(Utf8JsonWriter writer, string json, object? discriminatorValue,
        JsonSerializerOptions serializer)
    {
        string discriminatorName = JsonDiscriminatorPropertyName!;
        if (serializer.PropertyNamingPolicy != null)
        {
            discriminatorName = serializer.PropertyNamingPolicy.ConvertName(discriminatorName);
        }

        string discriminatorJson = JsonSerializer.Serialize(discriminatorValue, serializer);

        using JsonDocument document = JsonDocument.Parse(json);

        if (_addDiscriminatorFirst)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(discriminatorName);
            writer.WriteRawValue(discriminatorJson, skipInputValidation: true);
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (!property.NameEquals(discriminatorName))
                {
                    property.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStartObject();
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (!property.NameEquals(discriminatorName))
                {
                    property.WriteTo(writer);
                }
            }
            writer.WritePropertyName(discriminatorName);
            writer.WriteRawValue(discriminatorJson, skipInputValidation: true);
            writer.WriteEndObject();
        }
    }

    private static Action<Utf8JsonWriter, object, JsonSerializerOptions> BuildBaseTypeWriter(Type type)
    {
        BaseTypeWriteProperty[] properties =
        [
            .. type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod != null && !p.GetMethod.IsAbstract)
                .Select(p =>
                {
                    JsonPropertyNameAttribute? nameAttribute = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                    return new BaseTypeWriteProperty
                    {
                        Property = p,
                        JsonName = nameAttribute?.Name,
                        HasCustomName = nameAttribute != null,
                        JsonIgnore = p.GetCustomAttribute<JsonIgnoreAttribute>(),
                        DefaultValue = p.PropertyType.IsValueType ? Activator.CreateInstance(p.PropertyType) : null
                    };
                })
        ];

        return (writer, value, serializer) =>
        {
            writer.WriteStartObject();
            foreach (BaseTypeWriteProperty item in properties)
            {
                object? propertyValue = item.Property.GetValue(value);
                if (ShouldIgnore(item.DefaultValue, item.JsonIgnore, serializer.DefaultIgnoreCondition, propertyValue))
                {
                    continue;
                }

                string name = item.JsonName ?? item.Property.Name;
                if (!item.HasCustomName && serializer.PropertyNamingPolicy != null)
                {
                    name = serializer.PropertyNamingPolicy.ConvertName(name);
                }

                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, propertyValue, serializer);
            }
            writer.WriteEndObject();
        };
    }

    private readonly struct BaseTypeWriteProperty
    {
        public PropertyInfo Property { get; init; }
        public string? JsonName { get; init; }
        public bool HasCustomName { get; init; }
        public JsonIgnoreAttribute? JsonIgnore { get; init; }
        public object? DefaultValue { get; init; }
    }

    private static bool IsIgnoredOnRead(JsonIgnoreAttribute? jsonIgnore)
    {
        return jsonIgnore != null && jsonIgnore.Condition == JsonIgnoreCondition.Always;
    }

    private static bool ShouldIgnore(object? defaultValue, JsonIgnoreAttribute? jsonIgnore,
        JsonIgnoreCondition defaultIgnoreCondition, object? value)
    {
        if (jsonIgnore != null)
        {
            switch (jsonIgnore.Condition)
            {
                case JsonIgnoreCondition.Never:
                    return false;
                case JsonIgnoreCondition.WhenWritingNull:
                    return value == null;
                case JsonIgnoreCondition.WhenWritingDefault:
                    return IsDefaultValue(value, defaultValue);
                default:
                    return true;
            }
        }

        if (defaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
        {
            return value == null;
        }

        if (defaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
        {
            return IsDefaultValue(value, defaultValue);
        }

        return false;
    }

    private static bool IsDefaultValue(object? value, object? defaultValue)
    {
        return value == null || defaultValue != null && value.Equals(defaultValue);
    }

    private static readonly ConcurrentDictionary<Type, Func<object>>
        BaseTypeFactoryCache = new();

    private static T? ReadPlainObject(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions serializer)
    {
        JsonDocument jObject = JsonDocument.ParseValue(ref reader);
        object instance;
        try
        {
            Func<object> factory = BaseTypeFactoryCache.GetOrAdd(targetType, static type => () => Activator.CreateInstance(type)!);
            instance = factory();
        }
        catch (MissingMethodException)
        {
            throw new JsonException(
                $"Could not create an instance of type {targetType.FullName}: a parameterless constructor is required to fall back to the base type. Position: {reader.Position.GetInteger()}.");
        }

        Action<object, JsonElement, JsonSerializerOptions> readerFn =
            BaseTypeObjectReaderCache.GetOrAdd(targetType, static type => BuildBaseTypeObjectReader(type));
        readerFn(instance, jObject.RootElement, serializer);
        return (T)instance;
    }

    private static Action<object, JsonElement, JsonSerializerOptions> BuildBaseTypeObjectReader(Type type)
    {
        List<PropertyInfo> properties =
        [
            .. type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.SetMethod != null && !p.SetMethod.IsStatic &&
                            !IsIgnoredOnRead(p.GetCustomAttribute<JsonIgnoreAttribute>()))
        ];

        return (instance, element, options) =>
        {
            foreach (PropertyInfo property in properties)
            {
                string name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                if (options.PropertyNamingPolicy != null && property.GetCustomAttribute<JsonPropertyNameAttribute>() == null)
                {
                    name = options.PropertyNamingPolicy.ConvertName(name);
                }

                if (TryGetProperty(element, name, options, out JsonElement value))
                {
                    object? deserialized = value.Deserialize(property.PropertyType, options);
                    property.SetValue(instance, deserialized);
                }
            }
        };
    }

    private T? ReadJson(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
    {
        while (reader.TokenType == JsonTokenType.Comment)
        {
            reader.Read();
        }

        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return default;
            case JsonTokenType.StartObject:
                return ReadObject(ref reader, objectType, serializer);
            case JsonTokenType.StartArray:
            {
                Type? elementType = GetElementType(objectType);
                if (elementType == null)
                {
                    throw new JsonException($"Impossible to read JSON array to fill type: {objectType.Name}");
                }

                return (T)ReadArray(ref reader, objectType, elementType, serializer);
            }
            default:
                throw new JsonException($"Unrecognized token: {reader.TokenType}");
        }
    }

    private IList ReadArray(ref Utf8JsonReader reader, Type targetType, Type elementType,
        JsonSerializerOptions serializer)
    {
        IList list = CreateCompatibleList(targetType, elementType);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(ReadJson(ref reader, elementType, serializer));
        }

        if (!targetType.IsArray)
        {
            return list;
        }

        Array array = Array.CreateInstance(elementType, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    private static IList CreateCompatibleList(Type targetContainerType, Type elementType)
    {
        TypeInfo typeInfo = targetContainerType.GetTypeInfo();
        if (typeInfo.IsArray || typeInfo.IsAbstract)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        }

        return (IList)Activator.CreateInstance(targetContainerType)!;
    }

    private static Type? GetElementType(Type arrayOrGenericContainer)
    {
        if (arrayOrGenericContainer.IsArray)
        {
            return arrayOrGenericContainer.GetElementType();
        }

        IEnumerable<Type> genericTypeArguments = GetGenericTypeArguments(arrayOrGenericContainer);
        return genericTypeArguments.FirstOrDefault();
    }

    private T? ReadObject(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
    {
        Utf8JsonReader readerAtStart = reader;

        JsonDocument jObject = JsonDocument.ParseValue(ref reader);

        Type targetType = GetType(jObject, objectType, serializer);
        if (targetType == null || targetType.IsAbstract || targetType.IsInterface)
        {
            throw new JsonException(
                $"Could not create an instance of type {objectType.FullName}. Type is an interface or abstract class and cannot be instantiated. Position: {reader.Position.GetInteger()}.");
        }

        if (targetType == objectType)
        {
            return ReadPlainObject(ref readerAtStart, targetType, serializer);
        }

        return (T?)DeserializerHelper<T>.Deserialize(ref readerAtStart, targetType, serializer);
    }

    Type IJsonSubtypes.GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions jsonSerializerOptions)
    {
        Type? resolvedType;
        if (JsonDiscriminatorPropertyName == null)
        {
            resolvedType = GetTypeByPropertyPresence(jObject, parentType, jsonSerializerOptions);
        }
        else
        {
            resolvedType = GetTypeFromDiscriminatorValue(jObject, parentType, jsonSerializerOptions);
        }

        return resolvedType ?? GetFallbackSubType(parentType) ?? parentType;
    }

    private Type GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions serializer)
    {
        IJsonSubtypes[] converters = OptionsConverterCache.GetValue(serializer, static s =>
            [.. s.Converters.OfType<IJsonSubtypes>()]);

        Type targetType = parentType;
        IJsonSubtypes? currentTypeResolver = GetTypeResolver(targetType.GetTypeInfo(), converters);
        if (currentTypeResolver == null)
        {
            return targetType;
        }

        targetType = currentTypeResolver.GetType(jObject, targetType, serializer);
        if (targetType == parentType)
        {
            return targetType;
        }

        // Single-level resolution is the common case: only allocate the nested
        // walk (and its cycle-protection set) when the resolved type carries its
        // own resolver, i.e. for multi-level hierarchies.
        IJsonSubtypes? nestedResolver = GetTypeResolver(targetType.GetTypeInfo(),
            converters.Where(c => c != currentTypeResolver));
        if (nestedResolver == null)
        {
            return targetType;
        }

        IJsonSubtypes lastTypeResolver = currentTypeResolver;
        HashSet<Type> visitedTypes = [parentType, targetType];
        currentTypeResolver = nestedResolver;
        while (currentTypeResolver != null && currentTypeResolver != lastTypeResolver)
        {
            targetType = currentTypeResolver.GetType(jObject, targetType, serializer);
            if (!visitedTypes.Add(targetType))
            {
                break;
            }

            lastTypeResolver = currentTypeResolver;
            currentTypeResolver = GetTypeResolver(targetType.GetTypeInfo(),
                converters.Where(c => c != currentTypeResolver));
        }

        return targetType;
    }

    private IJsonSubtypes? GetTypeResolver(TypeInfo? targetType, IEnumerable<IJsonSubtypes> jsonConverterCollection)
    {
        if (targetType == null)
        {
            return null;
        }

        Type target = targetType.AsType();
        JsonSubTypeConverterAttribute? jsonConverterAttribute =
            ConverterAttributeCache.GetOrAdd(target, static type =>
                GetAttribute<JsonSubTypeConverterAttribute>(type.GetTypeInfo()));
        if (jsonConverterAttribute != null &&
            jsonConverterAttribute.ConverterType != null &&
            jsonConverterAttribute.ConverterType.IsGenericType &&
            jsonConverterAttribute.ConverterType.GenericTypeArguments.Length > 0 &&
            typeof(T).IsAssignableFrom(jsonConverterAttribute.ConverterType.GenericTypeArguments[0]))
        {
            return AttributeResolverCache.GetOrAdd((typeof(T), target),
                static key => CreateTypeResolver(key.Item2));
        }

        return jsonConverterCollection.FirstOrDefault(c => c.CanConvert(target));
    }

    private static IJsonSubtypes CreateTypeResolver(Type targetType)
    {
        JsonSubTypeConverterAttribute? attribute =
            ConverterAttributeCache.GetOrAdd(targetType, static type =>
                GetAttribute<JsonSubTypeConverterAttribute>(type.GetTypeInfo()));
        return (IJsonSubtypes)Activator.CreateInstance(attribute!.ConverterType!,
            attribute.DiscriminatorPropertyName)!;
    }

    private Type? GetTypeByPropertyPresence(JsonDocument jObject, Type parentType,
        JsonSerializerOptions jsonSerializerOptions)
    {
        IEnumerable<TypeWithPropertyMatchingAttributes> knownSubTypeAttributes =
            GetTypesByPropertyPresence(parentType);

        HashSet<Type> typesFound = [];
        foreach (TypeWithPropertyMatchingAttributes knownTypeItem in knownSubTypeAttributes)
        {
            if (!TryGetValueInJson(jObject.RootElement, knownTypeItem.JsonPropertyName, jsonSerializerOptions, out _))
            {
                continue;
            }

            if (knownTypeItem.StopLookupOnMatch)
            {
                return knownTypeItem.Type;
            }

            typesFound.Add(knownTypeItem.Type);
        }

        if (typesFound.Count == 1)
        {
            return typesFound.First();
        }

        if (typesFound.Count > 1)
        {
            throw new JsonException(
                "Ambiguous type resolution, expected only one type but got: " +
                string.Join(", ", [.. typesFound.Select(t => t.FullName)]));
        }

        return null;
    }

    internal virtual List<TypeWithPropertyMatchingAttributes> GetTypesByPropertyPresence(Type parentType)
    {
        if (_typesByPropertyPresence != null)
        {
            return _typesByPropertyPresence;
        }

        return
        [
            .. GetAttributes<KnownSubTypeWithPropertyAttribute>(parentType.GetTypeInfo())
                .Select(a => new TypeWithPropertyMatchingAttributes(a.SubType, a.PropertyName, a.StopLookupOnMatch))
        ];
    }

    private Type? GetTypeFromDiscriminatorValue(JsonDocument jObject, Type parentType,
        JsonSerializerOptions jsonSerializerOptions)
    {
        if (JsonDiscriminatorPropertyName == null ||
            !TryGetValueInJson(jObject.RootElement, JsonDiscriminatorPropertyName, jsonSerializerOptions,
                out JsonElement discriminatorValue))
        {
            return null;
        }

        NullableDictionary<object, Type> typeMapping = GetSubTypeMapping(parentType);
        if (typeMapping.Entries().Any())
        {
            return GetTypeFromMapping(typeMapping, discriminatorValue, jsonSerializerOptions);
        }

        string? discriminatorStringValue = discriminatorValue.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => discriminatorValue.GetString(),
            _ => discriminatorValue.ToString()
        };
        return GetTypeByName(discriminatorStringValue, parentType.GetTypeInfo());
    }

    private static bool TryGetValueInJson(JsonElement root, string propertyName,
        JsonSerializerOptions jsonSerializerOptions, out JsonElement value)
    {
        if (TryGetProperty(root, propertyName, jsonSerializerOptions, out value))
        {
            return true;
        }

        if (propertyName.IndexOf('.') >= 0)
        {
            string[] segments = propertyName.Split('.');
            JsonElement current = root;
            foreach (string segment in segments)
            {
                if (!TryGetProperty(current, segment, jsonSerializerOptions, out current))
                {
                    value = default;
                    return false;
                }
            }

            value = current;
            return true;
        }

        return false;
    }

    private static bool TryGetProperty(JsonElement obj, string name, JsonSerializerOptions jsonSerializerOptions,
        out JsonElement value)
    {
        if (obj.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (obj.TryGetProperty(name, out value))
        {
            return true;
        }

        string? convertedName = jsonSerializerOptions.PropertyNamingPolicy?.ConvertName(name);
        if (convertedName != null && convertedName != name && obj.TryGetProperty(convertedName, out value))
        {
            return true;
        }

        if (jsonSerializerOptions.PropertyNameCaseInsensitive)
        {
            foreach (JsonProperty jsonProperty in obj.EnumerateObject())
            {
                if (string.Equals(jsonProperty.Name, name, StringComparison.OrdinalIgnoreCase) ||
                    (convertedName != null && convertedName != name &&
                     string.Equals(jsonProperty.Name, convertedName, StringComparison.OrdinalIgnoreCase)))
                {
                    value = jsonProperty.Value;
                    return true;
                }
            }
        }

        return false;
    }

    private static Type? GetTypeByName(string? typeName, TypeInfo parentType)
    {
        if (typeName == null)
        {
            return null;
        }

        string? parentTypeFullName = parentType.FullName;
        string? searchLocation = parentTypeFullName == null
            ? null
            : parentTypeFullName.Substring(0, parentTypeFullName.Length - parentType.Name.Length);

        foreach (Assembly assembly in JsonSubTypesTypeResolution.GetSearchAssemblies(parentType.Assembly))
        {
            Type? typeByName = assembly.GetType(typeName);
            if (typeByName == null && searchLocation != null)
            {
                typeByName = assembly.GetType(searchLocation + typeName, false, true);
            }

            TypeInfo? typeByNameInfo = typeByName?.GetTypeInfo();
            if (typeByNameInfo != null && parentType.IsAssignableFrom(typeByNameInfo))
            {
                return typeByName;
            }
        }

        return null;
    }

    private static Type? GetTypeFromMapping(NullableDictionary<object, Type> typeMapping,
        JsonElement discriminatorToken, JsonSerializerOptions jsonSerializerOptions)
    {
        if (discriminatorToken.ValueKind == JsonValueKind.Null)
        {
            typeMapping.TryGetValue(null, out Type? targetType);

            return targetType;
        }

        object? key = typeMapping.NotNullKeys().FirstOrDefault();
        if (key != null)
        {
            // Fast path: for the dominant string/int mappings, compare the token directly
            // instead of round-tripping through GetRawText() + JsonSerializer.Deserialize.
            if (key is string && discriminatorToken.ValueKind == JsonValueKind.String)
            {
                string? stringValue = discriminatorToken.GetString();
                if (stringValue != null && typeMapping.TryGetValue(stringValue, out Type? stringTarget))
                {
                    return stringTarget;
                }

                return null;
            }

            if (key is int && discriminatorToken.TryGetInt32(out int intValue))
            {
                if (typeMapping.TryGetValue(intValue, out Type? intTarget))
                {
                    return intTarget;
                }

                return null;
            }

            Type targetLookupValueType = key.GetType();
            object? lookupValue;
            try
            {
                lookupValue = JsonSerializer.Deserialize(discriminatorToken.GetRawText(), targetLookupValueType,
                    jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }

            if (typeMapping.TryGetValue(lookupValue, out Type? targetType))
            {
                return targetType;
            }
        }

        return null;
    }

    internal virtual NullableDictionary<object, Type> GetSubTypeMapping(Type type)
    {
        if (_subTypeMapping != null)
        {
            return _subTypeMapping;
        }

        return BuildAttributeSubTypeMapping(type);
    }

    private static NullableDictionary<object, Type> BuildAttributeSubTypeMapping(Type type)
    {
        NullableDictionary<object, Type> dictionary = new();

        foreach (KnownSubTypeAttribute x in GetAttributes<KnownSubTypeAttribute>(type.GetTypeInfo()))
        {
            dictionary.Add(x.AssociatedValue, x.SubType);
        }

        return dictionary;
    }

    private static Dictionary<Type, object?>? BuildAttributeReverseMap(Type type)
    {
        NullableDictionary<object, Type> mapping = BuildAttributeSubTypeMapping(type);
        if (!mapping.Entries().Any())
        {
            return null;
        }

        Dictionary<Type, object?> reverse = new();
        foreach (KeyValuePair<object?, Type> entry in mapping.Entries())
        {
            reverse[entry.Value] = entry.Key;
        }

        return reverse;
    }

    internal virtual Type? GetFallbackSubType(Type type)
    {
        return _fallbackType ?? GetAttribute<FallBackSubTypeAttribute>(type.GetTypeInfo())?.SubType;
    }

    private static IEnumerable<TAttribute> GetAttributes<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute
    {
        foreach (object attribute in typeInfo.GetCustomAttributes(false))
        {
            if (attribute is TAttribute typed)
            {
                yield return typed;
            }
        }
    }

    private static TAttribute? GetAttribute<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute
    {
        return GetAttributes<TAttribute>(typeInfo).FirstOrDefault();
    }

    private static IEnumerable<Type> GetGenericTypeArguments(Type type)
    {
        return type.GenericTypeArguments;
    }
}
