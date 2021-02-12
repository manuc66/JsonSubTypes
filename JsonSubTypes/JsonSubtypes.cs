using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if (NET35 || NET40)
using TypeInfo = System.Type;
#else
using System.Reflection;
#endif

namespace JsonSubTypes
{
    //  MIT License
    //
    //  Copyright (c) 2017 Emmanuel Counasse
    //
    //  Permission is hereby granted, free of charge, to any person obtaining a copy
    //  of this software and associated documentation files (the "Software"), to deal
    //  in the Software without restriction, including without limitation the rights
    //  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    //  copies of the Software, and to permit persons to whom the Software is
    //  furnished to do so, subject to the following conditions:
    //
    //  The above copyright notice and this permission notice shall be included in all
    //  copies or substantial portions of the Software.
    //
    //  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    //  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    //  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    //  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    //  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    //  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    //  SOFTWARE.

    public class JsonSubtypes : JsonConverter
    {
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

        protected readonly string JsonDiscriminatorPropertyName;

        [ThreadStatic] private static bool _isInsideRead;

        [ThreadStatic] private static JsonReader _reader;

        public override bool CanRead
        {
            get
            {
                if (!_isInsideRead)
                    return true;

                return !string.IsNullOrEmpty(_reader.Path);
            }
        }

        public override bool CanWrite => false;

        public JsonSubtypes()
        {
        }

        public JsonSubtypes(string jsonDiscriminatorPropertyName)
        {
            JsonDiscriminatorPropertyName = jsonDiscriminatorPropertyName;
        }

        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            return ReadJson(reader, objectType, serializer);
        }

        private object ReadJson(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            while (reader.TokenType == JsonToken.Comment)
                reader.Read();

            object value;
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    value = null;
                    break;
                case JsonToken.StartObject:
                    value = ReadObject(reader, objectType, serializer);
                    break;
                case JsonToken.StartArray:
                    {
                        var elementType = GetElementType(objectType);
                        if (elementType == null)
                        {
                            throw CreateJsonReaderException(reader, $"Impossible to read JSON array to fill type: {objectType.Name}");
                        }
                        value = ReadArray(reader, objectType, elementType, serializer);
                        break;
                    }
                default:
                    throw CreateJsonReaderException(reader, $"Unrecognized token: {reader.TokenType}");
            }

            return value;
        }

        private static JsonReaderException CreateJsonReaderException(JsonReader reader, string message)
        {
            var lineNumber = 0;
            var linePosition = 0;
            if (reader is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                lineNumber = lineInfo.LineNumber;
                linePosition = lineInfo.LinePosition;
            }

            return new JsonReaderException(message, reader.Path, lineNumber, linePosition, null);
        }

        private IList ReadArray(JsonReader reader, Type targetType, Type elementType, JsonSerializer serializer)
        {
            var list = CreateCompatibleList(targetType, elementType);
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                list.Add(ReadJson(reader, elementType, serializer));
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

        private object ReadObject(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            // I would prefer to create a new reader, but can't work out how to
            // This seems a reasonable alternative - https://github.com/JamesNK/Newtonsoft.Json/issues/1605#issuecomment-602673364
            var savedDateParseSettings = reader.DateParseHandling;
            reader.DateParseHandling = DateParseHandling.None;
            var jObject = JObject.Load(reader);
            reader.DateParseHandling = savedDateParseSettings;

            var targetType = GetType(jObject, objectType, serializer) ?? objectType;

            return ThreadStaticReadObject(reader, serializer, jObject, targetType);
        }

        private static JsonReader CreateAnotherReader(JToken jToken, JsonReader reader)
        {
            var jObjectReader = jToken.CreateReader();
            jObjectReader.Culture = reader.Culture;
            jObjectReader.CloseInput = reader.CloseInput;
            jObjectReader.SupportMultipleContent = reader.SupportMultipleContent;
            jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
            jObjectReader.FloatParseHandling = reader.FloatParseHandling;
            jObjectReader.DateFormatString = reader.DateFormatString;
            jObjectReader.DateParseHandling = reader.DateParseHandling;
            return jObjectReader;
        }

        private Type ResolveType(JObject jObject, Type parentType, JsonSerializer serializer)
        {
            Type resolvedType;
            if (JsonDiscriminatorPropertyName == null)
            {
                resolvedType = GetTypeByPropertyPresence(jObject, parentType);
            }
            else
            {
                resolvedType = GetTypeFromDiscriminatorValue(jObject, parentType, serializer);
            }

            return resolvedType ?? GetFallbackSubType(parentType);
        }

        private Type GetType(JObject jObject, Type parentType, JsonSerializer serializer)
        {
            Type targetType = parentType;
            JsonSubtypes lastTypeResolver = null;
            JsonSubtypes currentTypeResolver = this;
            var visitedTypes = new HashSet<Type> { targetType };

            var jsonConverterCollection = serializer.Converters.OfType<JsonSubtypes>().ToList();
            while (currentTypeResolver != null && currentTypeResolver != lastTypeResolver)
            {
                targetType = currentTypeResolver.ResolveType(jObject, targetType, serializer);
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

        private JsonSubtypes GetTypeResolver(TypeInfo targetType, IEnumerable<JsonSubtypes> jsonConverterCollection)
        {
            if (targetType == null)
            {
                return null;
            }

            var jsonConverterAttribute = GetAttribute<JsonConverterAttribute>(targetType);
            if (jsonConverterAttribute != null && ToTypeInfo(typeof(JsonSubtypes)).IsAssignableFrom(ToTypeInfo(jsonConverterAttribute.ConverterType)))
            {
                return (JsonSubtypes)Activator.CreateInstance(jsonConverterAttribute.ConverterType, jsonConverterAttribute.ConverterParameters);
            }

            return jsonConverterCollection
                .FirstOrDefault(c => c.CanConvert(ToType(targetType)));
        }

        private Type GetTypeByPropertyPresence(JObject jObject, Type parentType)
        {
            var knownSubTypeAttributes = GetTypesByPropertyPresence(parentType);

            var types = knownSubTypeAttributes
                .Select(knownType =>
                {
                    if (TryGetValueInJson(jObject, knownType.Key, out JToken _))
                        return knownType.Value;

                    var token = jObject.SelectToken(knownType.Key);
                    if (token != null)
                    {
                        return knownType.Value;
                    }

                    return null;
                })
                .Where(type => type != null)
                .ToArray();

            if (types.Length == 1)
            {
                return types[0];
            }

            if (types.Length > 1)
            {
                throw new JsonSerializationException("Ambiguous type resolution, expected only one type but got: " + String.Join(", ", types.Select(t => t.FullName).ToArray()));
            }

            return null;
        }

        internal virtual Dictionary<string, Type> GetTypesByPropertyPresence(Type parentType)
        {
            return GetAttributes<KnownSubTypeWithPropertyAttribute>(ToTypeInfo(parentType))
                .ToDictionary(a => a.PropertyName, a => a.SubType);
        }

        private Type GetTypeFromDiscriminatorValue(JObject jObject, Type parentType, JsonSerializer serializer)
        {
            if (!TryGetValueInJson(jObject, JsonDiscriminatorPropertyName, out var discriminatorValue))
            {
                discriminatorValue = jObject.SelectToken(JsonDiscriminatorPropertyName);
            }

            if (discriminatorValue == null)
            {
                return null;
            }

            var typeMapping = GetSubTypeMapping(parentType);
            if (typeMapping.Entries().Any())
            {
                return GetTypeFromMapping(typeMapping, discriminatorValue, serializer);
            }

            return GetTypeByName(discriminatorValue.Value<string>(), ToTypeInfo(parentType));
        }

        private static bool TryGetValueInJson(IDictionary<string, JToken> jObject, string propertyName, out JToken value)
        {
            if (jObject.TryGetValue(propertyName, out value))
            {
                return true;
            }

            var matchingProperty = jObject
                .Keys
                .FirstOrDefault(jsonProperty => string.Equals(jsonProperty, propertyName, StringComparison.OrdinalIgnoreCase));

            if (matchingProperty == null)
            {
                return false;
            }

            value = jObject[matchingProperty];
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
            
            if (parentTypeFullName != null && typeByName == null)
            {
                typeByName = Type.GetType(typeName);
            }

            var typeByNameInfo = ToTypeInfo(typeByName);
            if (typeByNameInfo != null && parentType.IsAssignableFrom(typeByNameInfo))
            {
                return typeByName;
            }

            return null;
        }

        private static Type GetTypeFromMapping(NullableDictionary<object, Type> typeMapping, JToken discriminatorToken, JsonSerializer serializer)
        {
            if (discriminatorToken.Type == JTokenType.Null)
            {
                typeMapping.TryGetValue(null, out Type targetType);

                return targetType;
            }

            var key = typeMapping.NotNullKeys().FirstOrDefault();
            if (key != null)
            {
                var targetLookupValueType = key.GetType();
                var lookupValue = discriminatorToken.ToObject(targetLookupValueType, serializer);

                if (typeMapping.TryGetValue(lookupValue, out Type targetType))
                {
                    return targetType;
                }
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

        private static object ThreadStaticReadObject(JsonReader reader, JsonSerializer serializer, JToken jToken, Type targetType)
        {
            _reader = CreateAnotherReader(jToken, reader);
            _isInsideRead = true;
            try
            {
                return serializer.Deserialize(_reader, targetType);
            }
            finally
            {
                _isInsideRead = false;
            }
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
}
