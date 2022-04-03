using System;
using System.Collections;
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
        public string DiscriminatorPropertyName { get; }

        public JsonSubTypeConverterAttribute(Type converterType, string discriminatorPropertyName) : base(converterType)
        {
            DiscriminatorPropertyName = discriminatorPropertyName;
        }

        public JsonSubTypeConverterAttribute(Type converterType) : base(converterType)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class KnownSubTypeAttribute : Attribute
    {
        public Type SubType { get; }
        public object AssociatedValue { get; }

        public KnownSubTypeAttribute(Type subType, object associatedValue)
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

        public KnownSubTypeWithPropertyAttribute(Type subType, string propertyName)
        {
            SubType = subType;
            PropertyName = propertyName;
        }
    }

    public interface IJsonSubtypes
    {
        Type GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions jsonSerializerOptions);
        bool CanConvert(Type toType);
    }

    public class JsonSubtypes<T> : JsonConverter<T>, IJsonSubtypes
    {
        protected readonly string JsonDiscriminatorPropertyName;

        public JsonSubtypes()
        {
        }

        public JsonSubtypes(string jsonDiscriminatorPropertyName)
        {
            JsonDiscriminatorPropertyName = jsonDiscriminatorPropertyName;
        }


        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            return base.ReadAsPropertyName(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions serializer)
        {
            //JsonSerializer.Serialize(writer, value, serializer);
           JsonSerializer.Serialize<object>(writer, value, serializer);
         
            //throw new NotImplementedException();
        }


        public override T Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
        {
            return ReadJson(ref reader, objectType, serializer);
        }

        private T ReadJson(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
        {
            while (reader.TokenType == JsonTokenType.Comment)
                reader.Read();

            T value;
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    value = default;
                    break;
                case JsonTokenType.StartObject:
                    value = ReadObject(ref reader, objectType, serializer);
                    break;
                case JsonTokenType.StartArray:
                {
                    Type elementType = GetElementType(objectType);
                    if (elementType == null)
                    {
                        throw CreateJsonReaderException(ref reader,
                            $"Impossible to read JSON array to fill type: {objectType.Name}");
                    }

                    value = (T)ReadArray(ref reader, objectType, elementType, serializer);
                    break;
                }
                default:
                    throw CreateJsonReaderException(ref reader, $"Unrecognized token: {reader.TokenType}");
            }
            //    reader = beginnerReader;

            return value;
        }

        private static JsonException CreateJsonReaderException(ref Utf8JsonReader reader, string message)
        {
            return new JsonException(message);
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
                return list;

            Array array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        private static IList CreateCompatibleList(Type targetContainerType, Type elementType)
        {
            TypeInfo typeInfo = ToTypeInfo(targetContainerType);
            if (typeInfo.IsArray || typeInfo.IsAbstract)
            {
                return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            }

            return (IList)Activator.CreateInstance(targetContainerType);
        }

        private static Type GetElementType(Type arrayOrGenericContainer)
        {
            if (arrayOrGenericContainer.IsArray)
            {
                return arrayOrGenericContainer.GetElementType();
            }

            IEnumerable<Type> genericTypeArguments = GetGenericTypeArguments(arrayOrGenericContainer);
            return genericTypeArguments.FirstOrDefault();
        }

        private T ReadObject(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
        {
            // Copy the current state from reader (it's a struct)
            Utf8JsonReader readerAtStart = reader;

            JsonDocument jObject = JsonDocument.ParseValue(ref reader);

            Type targetType = GetType(jObject, objectType, serializer);
            if (targetType is null || targetType.IsAbstract)
            {
                throw new JsonException(
                    $"Could not create an instance of type {objectType.FullName}. Type is an interface or abstract class and cannot be instantiated. Position: {reader.Position.GetInteger()}.");
            }

            if (targetType != objectType)
            {
                return (T)DeserializerHelper<T>.Deserialize(ref readerAtStart, targetType, serializer);
            }
            else
            {
                throw new JsonException(
                    $"Could not deserialize to type {objectType.FullName}, no child type has been found. Position: {reader.Position.GetInteger()}.");

            }

            return default(T);
        }


        Type IJsonSubtypes.GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions jsonSerializerOptions)
        {
            Type resolvedType;
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
            IJsonSubtypes lastTypeResolver = null;
            IJsonSubtypes currentTypeResolver =
                GetTypeResolver(ToTypeInfo(targetType), serializer.Converters.OfType<IJsonSubtypes>());
            HashSet<Type> visitedTypes = new HashSet<Type> { targetType };

            List<IJsonSubtypes> jsonConverterCollection = serializer.Converters.OfType<IJsonSubtypes>().ToList();
            while (currentTypeResolver != null && currentTypeResolver != lastTypeResolver)
            {
                targetType = currentTypeResolver.GetType(jObject, targetType, serializer);
                if (!visitedTypes.Add(targetType))
                {
                    break;
                }

                lastTypeResolver = currentTypeResolver;
                jsonConverterCollection = jsonConverterCollection.Where(c => c != currentTypeResolver).ToList();
                currentTypeResolver = GetTypeResolver(ToTypeInfo(targetType), jsonConverterCollection);
            }

            return targetType;
        }

        private IJsonSubtypes GetTypeResolver(TypeInfo targetType, IEnumerable<IJsonSubtypes> jsonConverterCollection)
        {
            if (targetType == null)
            {
                return null;
            }

            JsonSubTypeConverterAttribute jsonConverterAttribute =
                GetAttribute<JsonSubTypeConverterAttribute>(targetType);
            if (jsonConverterAttribute != null && ToTypeInfo(typeof(T))
                    .IsAssignableFrom(ToTypeInfo(jsonConverterAttribute.ConverterType.GenericTypeArguments[0])))
            {
                return (JsonSubtypes<T>)Activator.CreateInstance(jsonConverterAttribute.ConverterType,
                    jsonConverterAttribute.DiscriminatorPropertyName);
            }

            return jsonConverterCollection
                .FirstOrDefault(c => c.CanConvert(ToType(targetType)));
        }

        private static Type GetTypeByPropertyPresence(JsonDocument jObject, Type parentType,
            JsonSerializerOptions jsonSerializerOptions)
        {
            IEnumerable<KnownSubTypeWithPropertyAttribute> knownSubTypeAttributes =
                GetAttributes<KnownSubTypeWithPropertyAttribute>(ToTypeInfo(parentType));

            return knownSubTypeAttributes
                .Select(knownType =>
                {
                    if (TryGetValueInJson(jObject, knownType.PropertyName, jsonSerializerOptions, out JsonElement _))
                        return knownType.SubType;

                    return null;
                })
                .FirstOrDefault(type => type != null);
        }

        private Type GetTypeFromDiscriminatorValue(JsonDocument jObject, Type parentType,
            JsonSerializerOptions jsonSerializerOptions)
        {
            if (!TryGetValueInJson(jObject, JsonDiscriminatorPropertyName, jsonSerializerOptions, out JsonElement discriminatorValue))
                return null;

            NullableDictionary<object, Type> typeMapping = GetSubTypeMapping(parentType);
            if (typeMapping.Entries().Any())
            {
                return GetTypeFromMapping(typeMapping, discriminatorValue);
            }

            string discriminatorStringValue = discriminatorValue.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => discriminatorValue.GetString(),
                _ => discriminatorValue.ToString()
            };
            return GetTypeByName(discriminatorStringValue, ToTypeInfo(parentType));
        }

        private static bool TryGetValueInJson(JsonDocument jObject, string propertyName,
            JsonSerializerOptions jsonSerializerOptions, out JsonElement value)
        {
            if (jObject.RootElement.TryGetProperty(propertyName, out value))
            {
                return true;
            }
            
            StringComparison comparisonType = jsonSerializerOptions.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            
            JsonElement.ObjectEnumerator objectEnumerator = jObject
                .RootElement
                .EnumerateObject();
            foreach (JsonProperty jsonProperty in objectEnumerator.Where(jsonProperty =>
                     {
                   
                         return string.Equals(jsonProperty.Name, propertyName, comparisonType);
                     }))
            {
                value = jsonProperty.Value;
                return true;
            }

            return false;
        }

        private static Type GetTypeByName(string typeName, TypeInfo parentType)
        {
            if (typeName == null)
            {
                return null;
            }

            Assembly insideAssembly = parentType.Assembly;

            string parentTypeFullName = parentType.FullName;

            Type typeByName = insideAssembly.GetType(typeName);
            if (parentTypeFullName != null && typeByName == null)
            {
                string searchLocation =
                    parentTypeFullName.Substring(0, parentTypeFullName.Length - parentType.Name.Length);
                typeByName = insideAssembly.GetType(searchLocation + typeName, false, true);
            }

            TypeInfo typeByNameInfo = ToTypeInfo(typeByName);
            if (typeByNameInfo != null && parentType.IsAssignableFrom(typeByNameInfo))
            {
                return typeByName;
            }

            return null;
        }

        private static Type GetTypeFromMapping(NullableDictionary<object, Type> typeMapping,
            JsonElement discriminatorToken)
        {
            if (discriminatorToken.ValueKind == JsonValueKind.Null)
            {
                typeMapping.TryGetValue(null, out Type targetType);

                return targetType;
            }

            object key = typeMapping.NotNullKeys().FirstOrDefault();
            if (key != null)
            {
                Type targetLookupValueType = key.GetType();
                object lookupValue = JsonSerializer.Deserialize(discriminatorToken.GetRawText(), targetLookupValueType);

                if (typeMapping.TryGetValue(lookupValue, out Type targetType))
                {
                    return targetType;
                }
            }

            return null;
        }

        internal virtual NullableDictionary<object, Type> GetSubTypeMapping(Type type)
        {
            NullableDictionary<object, Type> dictionary = new NullableDictionary<object, Type>();

            GetAttributes<KnownSubTypeAttribute>(ToTypeInfo(type))
                .ToList()
                .ForEach(x => dictionary.Add(x.AssociatedValue, x.SubType));

            return dictionary;
        }

        internal virtual Type GetFallbackSubType(Type type)
        {
            return GetAttribute<FallBackSubTypeAttribute>(ToTypeInfo(type))?.SubType;
        }


        private static IEnumerable<T> GetAttributes<T>(TypeInfo typeInfo) where T : Attribute
        {
            return typeInfo.GetCustomAttributes(false)
                .OfType<T>();
        }

        private static T GetAttribute<T>(TypeInfo typeInfo) where T : Attribute
        {
            return GetAttributes<T>(typeInfo).FirstOrDefault();
        }

        private static IEnumerable<Type> GetGenericTypeArguments(Type type)
        {
#if (NET35 || NET40)
            return type.GetGenericArguments();
#else
            return type.GenericTypeArguments;
#endif
        }

        internal static TypeInfo ToTypeInfo(Type type)
        {
#if (NET35 || NET40)
            return type;
#else
            return type?.GetTypeInfo();
#endif
        }

        internal static Type ToType(TypeInfo typeInfo)
        {
#if (NET35 || NET40)
            return typeInfo;
#else
            return typeInfo?.AsType();
#endif
        }
    }

    public class NullableDictionary<TKey, TValue>
    {
        private bool _hasNullKey;
        private TValue _nullKeyValue;
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (Equals(key, default(TKey)))
            {
                if (!_hasNullKey)
                {
                    value = default(TValue);
                    return false;
                }

                value = _nullKeyValue;
                return true;
            }

            return _dictionary.TryGetValue(key, out value);
        }

        public void Add(TKey key, TValue value)
        {
            if (Equals(key, default(TKey)))
            {
                if (_hasNullKey)
                {
                    throw new ArgumentException();
                }

                _hasNullKey = true;
                _nullKeyValue = value;
            }
            else
            {
                _dictionary.Add(key, value);
            }
        }

        public IEnumerable<TKey> NotNullKeys()
        {
            return _dictionary.Keys;
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> Entries()
        {
            if (_hasNullKey)
            {
                yield return new KeyValuePair<TKey, TValue>(default(TKey), _nullKeyValue);
            }

            foreach (KeyValuePair<TKey, TValue> value in _dictionary)
            {
                yield return value;
            }
        }
    }
}
