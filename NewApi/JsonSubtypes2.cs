using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewApi
{
    public class JsonSubTypeConverterAttribute : JsonConverterAttribute
    {
        public string DiscriminatorPropertyName { get; }

        public JsonSubTypeConverterAttribute(Type converterType, string discriminatorPropertyName) : base(converterType)
        {
            DiscriminatorPropertyName = discriminatorPropertyName;
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

    public class JsonSubtypes<T> : JsonConverter<T>
    {

        protected readonly string JsonDiscriminatorPropertyName;

        [ThreadStatic] private static bool _isInsideRead;



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

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions serializer)
        {
            throw new NotImplementedException();
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
                        var elementType = GetElementType(objectType);
                        if (elementType == null)
                        {
                            throw CreateJsonReaderException(ref reader, $"Impossible to read JSON array to fill type: {objectType.Name}");
                        }
                        value = (T)ReadArray(ref reader, objectType, elementType, serializer);
                        break;
                    }
                default:
                    throw CreateJsonReaderException(ref reader, $"Unrecognized token: {reader.TokenType}");
            }

            return value;
        }

        private static InvalidOperationException CreateJsonReaderException(ref Utf8JsonReader reader, string message)
        {
            return new InvalidOperationException(message);
        }

        private IList ReadArray(ref Utf8JsonReader reader, Type targetType, Type elementType, JsonSerializerOptions serializer)
        {
            var list = CreateCompatibleList(targetType, elementType);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                list.Add(ReadJson(ref reader, elementType, serializer));
            }

            if (!targetType.IsArray)
                return list;

            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        private static IList CreateCompatibleList(Type targetContainerType, Type elementType)
        {
            var typeInfo = ToTypeInfo(targetContainerType);
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

            var genericTypeArguments = GetGenericTypeArguments(arrayOrGenericContainer);
            return genericTypeArguments.FirstOrDefault();
        }

        private T ReadObject(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions serializer)
        {
            var jObject = JsonDocument.ParseValue(ref reader);

            var targetType = GetType(jObject, objectType, serializer) ?? objectType;

            return (T)JsonSerializer.Deserialize(jObject.RootElement.GetRawText(), targetType);
        }

        private Type GetType(JsonDocument jObject, Type parentType)
        {
            Type resolvedType;
            if (JsonDiscriminatorPropertyName == null)
            {
                resolvedType = GetTypeByPropertyPresence(jObject, parentType);
            }
            else
            {
                resolvedType = GetTypeFromDiscriminatorValue(jObject, parentType);
            }

            return resolvedType ?? GetFallbackSubType(parentType);
        }

        private Type GetType(JsonDocument jObject, Type parentType, JsonSerializerOptions serializer)
        {
            Type targetType = parentType;
            JsonSubtypes<T> lastTypeResolver = null;
            JsonSubtypes<T> currentTypeResolver = this;

            var jsonConverterCollection = serializer.Converters.OfType<JsonSubtypes<T>>().ToList();
            while (currentTypeResolver != null && currentTypeResolver != lastTypeResolver)
            {
                targetType = currentTypeResolver.GetType(jObject, targetType);
                lastTypeResolver = currentTypeResolver;
                jsonConverterCollection = jsonConverterCollection.Where(c => c != currentTypeResolver).ToList();
                currentTypeResolver = GetTypeResolver(ToTypeInfo(targetType), jsonConverterCollection);
            }

            return targetType;
        }

        private JsonSubtypes<T> GetTypeResolver(TypeInfo targetType, IEnumerable<JsonSubtypes<T>> jsonConverterCollection)
        {
            if (targetType == null)
            {
                return null;
            }

            var jsonConverterAttribute = GetAttribute<JsonSubTypeConverterAttribute>(targetType);
            if (jsonConverterAttribute != null && ToTypeInfo(typeof(JsonSubtypes<T>)).IsAssignableFrom(ToTypeInfo(jsonConverterAttribute.ConverterType)))
            {
                return (JsonSubtypes<T>)Activator.CreateInstance(jsonConverterAttribute.ConverterType, jsonConverterAttribute.DiscriminatorPropertyName);
            }

            return jsonConverterCollection
                .FirstOrDefault(c => c.CanConvert(ToType(targetType)));
        }

        private static Type GetTypeByPropertyPresence(JsonDocument jObject, Type parentType)
        {
            var knownSubTypeAttributes = GetAttributes<KnownSubTypeWithPropertyAttribute>(ToTypeInfo(parentType));

            return knownSubTypeAttributes
                .Select(knownType =>
                {
                    if (TryGetValueInJson(jObject, knownType.PropertyName, out JsonElement _))
                        return knownType.SubType;

                    return null;
                })
                .FirstOrDefault(type => type != null);
        }

        private Type GetTypeFromDiscriminatorValue(JsonDocument jObject, Type parentType)
        {
            if (!TryGetValueInJson(jObject, JsonDiscriminatorPropertyName, out var discriminatorValue))
                return null;

            var typeMapping = GetSubTypeMapping(parentType);
            if (typeMapping.Entries().Any())
            {
                return GetTypeFromMapping(typeMapping, discriminatorValue);
            }

            return GetTypeByName(discriminatorValue.GetString(), ToTypeInfo(parentType));
        }

        private static bool TryGetValueInJson(JsonDocument jObject, string propertyName, out JsonElement value)
        {
            if (jObject.RootElement.TryGetProperty(propertyName, out value))
            {
                return true;
            }

            JsonProperty matchingProperty = jObject
                .RootElement
                .EnumerateObject()
                .FirstOrDefault(jsonProperty => string.Equals(jsonProperty.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(matchingProperty.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            value = matchingProperty.Value;
            return true;
        }

        private static Type GetTypeByName(string typeName, TypeInfo parentType)
        {
            if (typeName == null)
            {
                return null;
            }

            var insideAssembly = parentType.Assembly;

            var parentTypeFullName = parentType.FullName;

            var typeByName = insideAssembly.GetType(typeName);
            if (parentTypeFullName != null && typeByName == null)
            {
                var searchLocation = parentTypeFullName.Substring(0, parentTypeFullName.Length - parentType.Name.Length);
                typeByName = insideAssembly.GetType(searchLocation + typeName, false, true);
            }

            var typeByNameInfo = ToTypeInfo(typeByName);
            if (typeByNameInfo != null && parentType.IsAssignableFrom(typeByNameInfo))
            {
                return typeByName;
            }

            return null;
        }

        private static Type GetTypeFromMapping(NullableDictionary<object, Type> typeMapping, JsonElement discriminatorToken)
        {
            if (discriminatorToken.ValueKind == JsonValueKind.Null)
            {
                typeMapping.TryGetValue(null, out Type targetType);

                return targetType;
            }

            var key = typeMapping.NotNullKeys().FirstOrDefault();
            if (key != null)
            {
                throw new NotImplementedException();
                //var targetLookupValueType = key.GetType();
                //var lookupValue = discriminatorToken.ToObject(targetLookupValueType);

                //if (typeMapping.TryGetValue(lookupValue, out Type targetType))
                //{
                //    return targetType;
                //}
            }

            return null;
        }

        internal virtual NullableDictionary<object, Type> GetSubTypeMapping(Type type)
        {
            var dictionary = new NullableDictionary<object, Type>();

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

            foreach (var value in _dictionary)
            {
                yield return value;
            }
        }
    }
}
