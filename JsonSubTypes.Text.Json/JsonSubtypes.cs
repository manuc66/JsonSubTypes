using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonSubTypes.Text.Json
{
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

        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            return DiscriminatorPropertyName == null
                ? (JsonConverter)Activator.CreateInstance(ConverterType!)!
                : (JsonConverter)Activator.CreateInstance(ConverterType!, DiscriminatorPropertyName)!;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class KnownSubTypeAttribute : Attribute
    {
        public Type SubType { get; }
        public object? AssociatedValue { get; }

        public KnownSubTypeAttribute(Type subType, object? associatedValue)
        {
            SubType = subType;
            AssociatedValue = associatedValue;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class FallBackSubTypeAttribute : Attribute
    {
        public Type SubType { get; }

        public FallBackSubTypeAttribute(Type subType)
        {
            SubType = subType;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class KnownSubTypeWithPropertyAttribute : Attribute
    {
        public Type SubType { get; }
        public string PropertyName { get; }
        public bool StopLookupOnMatch { get; set; }

        public KnownSubTypeWithPropertyAttribute(Type subType, string propertyName)
        {
            SubType = subType;
            PropertyName = propertyName;
        }
    }

    internal interface IJsonSubtypes
    {
        Type GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions jsonSerializerOptions);
        bool CanConvert(Type toType);
    }

    public class JsonSubtypes<T> : JsonConverter<T>, IJsonSubtypes
    {
        private static readonly ConcurrentDictionary<Type, Action<Utf8JsonWriter, object, JsonSerializerOptions>>
            BaseTypeWriterCache = new ConcurrentDictionary<Type, Action<Utf8JsonWriter, object, JsonSerializerOptions>>();

        private static readonly ConcurrentDictionary<Type, Action<object, JsonElement, JsonSerializerOptions>>
            BaseTypeObjectReaderCache = new ConcurrentDictionary<Type, Action<object, JsonElement, JsonSerializerOptions>>();

        protected readonly string? JsonDiscriminatorPropertyName;

        private readonly NullableDictionary<object, Type>? _subTypeMapping;
        private readonly List<TypeWithPropertyMatchingAttributes>? _typesByPropertyPresence;
        private readonly Type? _fallbackType;

        public JsonSubtypes()
        {
        }

        public JsonSubtypes(string? jsonDiscriminatorPropertyName)
        {
            JsonDiscriminatorPropertyName = jsonDiscriminatorPropertyName;
        }

        internal JsonSubtypes(string? jsonDiscriminatorPropertyName,
            NullableDictionary<object, Type>? subTypeMapping,
            List<TypeWithPropertyMatchingAttributes>? typesByPropertyPresence,
            Type? fallbackType) : this(jsonDiscriminatorPropertyName)
        {
            _subTypeMapping = subTypeMapping;
            _typesByPropertyPresence = typesByPropertyPresence;
            _fallbackType = fallbackType;
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

            if (value.GetType() != typeof(T))
            {
                JsonSerializer.Serialize<object>(writer, value, serializer);
                return;
            }

            Action<Utf8JsonWriter, object, JsonSerializerOptions> baseWriter =
                BaseTypeWriterCache.GetOrAdd(typeof(T), static type => BuildBaseTypeWriter(type));
            baseWriter(writer, value, serializer);
        }

        private static Action<Utf8JsonWriter, object, JsonSerializerOptions> BuildBaseTypeWriter(Type type)
        {
            PropertyInfo[] properties = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod != null && !p.GetMethod.IsAbstract)
                .ToArray();

            return (writer, value, serializer) =>
            {
                writer.WriteStartObject();
                foreach (PropertyInfo property in properties)
                {
                    string name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;
                    if (serializer.PropertyNamingPolicy != null && property.GetCustomAttribute<JsonPropertyNameAttribute>() == null)
                    {
                        name = serializer.PropertyNamingPolicy.ConvertName(name);
                    }

                    object? propertyValue = property.GetValue(value);
                    writer.WritePropertyName(name);
                    JsonSerializer.Serialize(writer, propertyValue, serializer);
                }
                writer.WriteEndObject();
            };
        }

        private static T? ReadPlainObject(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions serializer)
        {
            JsonDocument jObject = JsonDocument.ParseValue(ref reader);
            object instance = Activator.CreateInstance(targetType)!;
            Action<object, JsonElement, JsonSerializerOptions> readerFn =
                BaseTypeObjectReaderCache.GetOrAdd(targetType, static type => BuildBaseTypeObjectReader(type));
            readerFn(instance, jObject.RootElement, serializer);
            return (T)instance;
        }

        private static Action<object, JsonElement, JsonSerializerOptions> BuildBaseTypeObjectReader(Type type)
        {
            List<PropertyInfo> properties = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.SetMethod != null && !p.SetMethod.IsStatic &&
                            p.GetCustomAttribute<JsonIgnoreAttribute>() == null)
                .ToList();

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
                        object? deserialized = JsonSerializer.Deserialize(value.GetRawText(), property.PropertyType, options);
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
            TypeInfo typeInfo = ToTypeInfo(targetContainerType)!;
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
            Type targetType = parentType;
            IJsonSubtypes? lastTypeResolver = null;
            List<IJsonSubtypes> converters = serializer.Converters.OfType<IJsonSubtypes>().ToList();
            IJsonSubtypes? currentTypeResolver = GetTypeResolver(ToTypeInfo(targetType), converters);
            HashSet<Type> visitedTypes = new HashSet<Type> { targetType };

            while (currentTypeResolver != null && currentTypeResolver != lastTypeResolver)
            {
                targetType = currentTypeResolver.GetType(jObject, targetType, serializer);
                if (!visitedTypes.Add(targetType))
                {
                    break;
                }

                lastTypeResolver = currentTypeResolver;
                converters = converters.Where(c => c != currentTypeResolver).ToList();
                currentTypeResolver = GetTypeResolver(ToTypeInfo(targetType), converters);
            }

            return targetType;
        }

        private IJsonSubtypes? GetTypeResolver(TypeInfo? targetType, IEnumerable<IJsonSubtypes> jsonConverterCollection)
        {
            if (targetType == null)
            {
                return null;
            }

            JsonSubTypeConverterAttribute? jsonConverterAttribute =
                GetAttribute<JsonSubTypeConverterAttribute>(targetType);
            if (jsonConverterAttribute != null &&
                typeof(T).IsAssignableFrom(jsonConverterAttribute.ConverterType!.GenericTypeArguments[0]))
            {
                return (IJsonSubtypes)Activator.CreateInstance(jsonConverterAttribute.ConverterType,
                    jsonConverterAttribute.DiscriminatorPropertyName)!;
            }

            return jsonConverterCollection.FirstOrDefault(c => c.CanConvert(ToType(targetType)));
        }

        private Type? GetTypeByPropertyPresence(JsonDocument jObject, Type parentType,
            JsonSerializerOptions jsonSerializerOptions)
        {
            IEnumerable<TypeWithPropertyMatchingAttributes> knownSubTypeAttributes =
                GetTypesByPropertyPresence(parentType);

            HashSet<Type> typesFound = new HashSet<Type>();
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
                    string.Join(", ", typesFound.Select(t => t.FullName).ToArray()));
            }

            return null;
        }

        internal virtual List<TypeWithPropertyMatchingAttributes> GetTypesByPropertyPresence(Type parentType)
        {
            if (_typesByPropertyPresence != null)
            {
                return _typesByPropertyPresence;
            }

            return GetAttributes<KnownSubTypeWithPropertyAttribute>(ToTypeInfo(parentType)!)
                .Select(a => new TypeWithPropertyMatchingAttributes(a.SubType, a.PropertyName, a.StopLookupOnMatch))
                .ToList();
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
            return GetTypeByName(discriminatorStringValue, ToTypeInfo(parentType)!);
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
            bool hasConvertedName = convertedName != null && convertedName != name;
            if (hasConvertedName && obj.TryGetProperty(convertedName!, out value))
            {
                return true;
            }

            if (jsonSerializerOptions.PropertyNameCaseInsensitive)
            {
                foreach (JsonProperty jsonProperty in obj.EnumerateObject())
                {
                    if (string.Equals(jsonProperty.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        (hasConvertedName &&
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

            Assembly insideAssembly = parentType.Assembly;

            string? parentTypeFullName = parentType.FullName;

            Type? typeByName = insideAssembly.GetType(typeName);
            if (parentTypeFullName != null && typeByName == null)
            {
                string searchLocation =
                    parentTypeFullName.Substring(0, parentTypeFullName.Length - parentType.Name.Length);
                typeByName = insideAssembly.GetType(searchLocation + typeName, false, true);
            }

            TypeInfo? typeByNameInfo = ToTypeInfo(typeByName);
            if (typeByNameInfo != null && parentType.IsAssignableFrom(typeByNameInfo))
            {
                return typeByName;
            }

            return null;
        }

        private static Type? GetTypeFromMapping(NullableDictionary<object, Type> typeMapping,
            JsonElement discriminatorToken, JsonSerializerOptions jsonSerializerOptions)
        {
            if (discriminatorToken.ValueKind == JsonValueKind.Null)
            {
                typeMapping.TryGetValue(null!, out Type? targetType);

                return targetType;
            }

            object? key = typeMapping.NotNullKeys().FirstOrDefault();
            if (key != null)
            {
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

            NullableDictionary<object, Type> dictionary = new NullableDictionary<object, Type>();

            foreach (KnownSubTypeAttribute x in GetAttributes<KnownSubTypeAttribute>(ToTypeInfo(type)!))
            {
                dictionary.Add(x.AssociatedValue, x.SubType);
            }

            return dictionary;
        }

        internal virtual Type? GetFallbackSubType(Type type)
        {
            return _fallbackType ?? GetAttribute<FallBackSubTypeAttribute>(ToTypeInfo(type)!)?.SubType;
        }

        private static IEnumerable<TAttribute> GetAttributes<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute
        {
            return typeInfo.GetCustomAttributes(false).OfType<TAttribute>();
        }

        private static TAttribute? GetAttribute<TAttribute>(TypeInfo typeInfo) where TAttribute : Attribute
        {
            return GetAttributes<TAttribute>(typeInfo).FirstOrDefault();
        }

        private static IEnumerable<Type> GetGenericTypeArguments(Type type)
        {
            return type.GenericTypeArguments;
        }

        internal static TypeInfo? ToTypeInfo(Type? type)
        {
            return type?.GetTypeInfo();
        }

        internal static Type ToType(TypeInfo typeInfo)
        {
            return typeInfo.AsType();
        }
    }
}
