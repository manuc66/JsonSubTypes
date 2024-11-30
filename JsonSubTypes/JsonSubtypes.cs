using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if (!NETSTANDARD1_3)
using TypeInfo = System.Type;

#else
using System.Reflection;
#endif
#if !NET35
using System.Collections.Concurrent;
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

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
        public class KnownBaseTypeAttribute : Attribute
        {
            public Type BaseType { get; }
            public object AssociatedValue { get; }

            public KnownBaseTypeAttribute(Type baseType, object associatedValue)
            {
                BaseType = baseType;
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

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
        public class KnownBaseTypeWithPropertyAttribute : Attribute
        {
            public Type BaseType { get; }
            public string PropertyName { get; }
            public bool StopLookupOnMatch { get; set; }

            public KnownBaseTypeWithPropertyAttribute(Type baseType, string propertyName)
            {
                BaseType = baseType;
                PropertyName = propertyName;
            }
        }

        protected readonly string JsonDiscriminatorPropertyName;

        [ThreadStatic] private static bool _isInsideRead;

        [ThreadStatic] private static JsonReader _reader;

#if NET35
        private static readonly Dictionary<TypeInfo, IEnumerable<object>> _attributesCache = new Dictionary<TypeInfo, IEnumerable<object>>();
        private static readonly Dictionary<TypeInfo, IEnumerable<Type>> _typesWithKnownBaseTypeAttributesCache = new Dictionary<TypeInfo, IEnumerable<Type>>();
#else
        private static readonly ConcurrentDictionary<TypeInfo, IEnumerable<object>> _attributesCache = new ConcurrentDictionary<TypeInfo, IEnumerable<object>>();
        private static readonly Func<TypeInfo, IEnumerable<object>> _getCustomAttributes = ti => ti.GetCustomAttributes(false);
        private static readonly ConcurrentDictionary<TypeInfo, IEnumerable<Type>> _typesWithKnownBaseTypeAttributesCache = new ConcurrentDictionary<TypeInfo, IEnumerable<Type>>();
#endif

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

        private static readonly TypeInfo _knownBaseTypeAttributeType = ToTypeInfo(typeof(KnownBaseTypeAttribute));
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
            var prevDateParseHandling = reader.DateParseHandling;
            reader.DateParseHandling = DateParseHandling.None;
            var jObject = JObject.Load(reader);
            reader.DateParseHandling = prevDateParseHandling;

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
            jObjectReader.MaxDepth = reader.MaxDepth;
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

            HashSet<Type> typesFound = new HashSet<Type>();
            foreach (TypeWithPropertyMatchingAttributes knownTypeItem in knownSubTypeAttributes)
            {
                Type matchingKnownType = null;
                if (TryGetValueInJson(jObject, knownTypeItem.JsonPropertyName, out JToken _))
                {
                    matchingKnownType = knownTypeItem.Type;
                }
                else
                {
                    JToken token = jObject.SelectToken(knownTypeItem.JsonPropertyName);
                    if (token != null)
                    {
                        matchingKnownType = knownTypeItem.Type;
                    }
                }

                if (matchingKnownType != null)
                {
                    if (knownTypeItem.StopLookupOnMatch)
                    {
                        return knownTypeItem.Type;
                    }
                    typesFound.Add(matchingKnownType);
                }
            }

            if (typesFound.Count == 1)
            {
                return typesFound.First();
            }

            if (typesFound.Count > 1)
            {
                throw new JsonSerializationException("Ambiguous type resolution, expected only one type but got: " + String.Join(", ", typesFound.Select(t => t.FullName).ToArray()));
            }

            return null;
        }

        internal virtual List<TypeWithPropertyMatchingAttributes> GetTypesByPropertyPresence(Type parentType)
        {
            return GetAttributes<KnownSubTypeWithPropertyAttribute>(ToTypeInfo(parentType))
                .Select(a => new TypeWithPropertyMatchingAttributes(a.SubType, a.PropertyName, a.StopLookupOnMatch))
                .Concat(
                    FindTypesWithAttribute(ToTypeInfo(typeof(KnownBaseTypeWithPropertyAttribute)))
                    .Where(x => GetAttributes<KnownBaseTypeWithPropertyAttribute>(ToTypeInfo(x)).Any(t => t.BaseType == parentType))
                    .Select(x =>
                    {
                        var firstAttribute = GetAttributes<KnownBaseTypeWithPropertyAttribute>(ToTypeInfo(x))
                                                        .Where(t => t.BaseType == parentType)
                                                        .First();
                        return new TypeWithPropertyMatchingAttributes(x, firstAttribute.PropertyName, firstAttribute.StopLookupOnMatch);
                    })
                )
                .ToList();
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

            TypeInfo typeByNameInfo = ToTypeInfo(typeByName);
            if (typeByNameInfo == null)
            {
                return null;
            }

            if (parentType.IsAssignableFrom(typeByNameInfo) || (parentType.IsGenericType && IsSubclassOfRawGeneric(parentType, typeByNameInfo)))
            {
                return typeByName;
            }

            return null;
        }

        static bool IsSubclassOfRawGeneric(TypeInfo generic, TypeInfo toCheck)
        {
            TypeInfo cur = toCheck;
            TypeInfo objectTypeInfo = ToTypeInfo(typeof(object));
            while (generic != cur && toCheck != objectTypeInfo)
            {
                cur = toCheck.IsGenericType ? ToTypeInfo(toCheck.GetGenericTypeDefinition()) : toCheck;
                toCheck = ToTypeInfo(toCheck.BaseType);
            }

            return generic == cur;
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

            var typeAsTypeInfo = ToTypeInfo(type);

            GetAttributes<KnownSubTypeAttribute>(typeAsTypeInfo)
                .ToList()
                .ForEach(x => dictionary.Add(x.AssociatedValue, x.SubType));

            FindTypesWithAttribute(_knownBaseTypeAttributeType)
            .Where(x => GetAttributes<KnownBaseTypeAttribute>(typeAsTypeInfo).Any(t => t.BaseType == type))
            .Select(x => new
            {
                AssociatedValue = GetAttributes<KnownBaseTypeAttribute>(typeAsTypeInfo)
                                            .Where(t => t.BaseType == type)
                                            .Select(t => t.AssociatedValue)
                                            .First(),
                SubType = x
            })
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

        private static IEnumerable<object> GetAttributes(TypeInfo typeInfo)
        {
#if NET35
            lock (_attributesCache)
            {
                if (_attributesCache.TryGetValue(typeInfo, out var res))
                    return res;

                res = typeInfo.GetCustomAttributes(false);
                _attributesCache.Add(typeInfo, res);

                return res;
            }
#else
            return _attributesCache.GetOrAdd(typeInfo, _getCustomAttributes);
#endif
        }

        private static IEnumerable<T> GetAttributes<T>(TypeInfo typeInfo) where T : Attribute
        {
            return GetAttributes(typeInfo)
                .OfType<T>();
        }

        private static T GetAttribute<T>(TypeInfo typeInfo) where T : Attribute
        {
            return GetAttributes<T>(typeInfo).FirstOrDefault();
        }

        private static IEnumerable<Type> _getTypesWithCustomAttribute(TypeInfo attributeType)
        {
#if NETSTANDARD1_3
                return new Type[0];
#else
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<IEnumerable<Type>> typesInAssemblies = new List<IEnumerable<TypeInfo>>();
            foreach (var assembly in assemblies)
            {
                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetTypes();
                }
                //For example: Microsoft.Graph, combined with PnP.Framework can throw these.
                //In my testing, combining PnP.Framework v1.8.0 and Microsoft.Graph v5.38.0 gives the following Exception:
                //System.Reflection.ReflectionTypeLoadException: Unable to load one or more of the requested types.
                //Could not load type 'Microsoft.Graph.HttpProvider' from assembly 'Microsoft.Graph.Core, Version=3.1.3.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'.
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(x => x != null);
                }
                var filteredTypes = types
                        .Where(t => t.GetCustomAttributes(attributeType, false).Any());

                typesInAssemblies.Add(filteredTypes);
            }
            return typesInAssemblies.SelectMany(x => x).ToList();
#endif
        }

        private static IEnumerable<Type> FindTypesWithAttribute(TypeInfo attributeType)
        {
#if NET35
            lock (_attributesCache)
            {
                if (_typesWithKnownBaseTypeAttributesCache.TryGetValue(attributeType, out var res))
                    return res;

                res = _getTypesWithCustomAttribute(attributeType);
                _typesWithKnownBaseTypeAttributesCache.Add(attributeType, res);

                return res;
            }
#else
            return _typesWithKnownBaseTypeAttributesCache.GetOrAdd(attributeType, _getTypesWithCustomAttribute);
#endif
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
#if (!NETSTANDARD1_3)
            return type;
#else
            return type?.GetTypeInfo();
#endif
        }

        internal static Type ToType(TypeInfo typeInfo)
        {
#if (!NETSTANDARD1_3)
            return typeInfo;
#else
            return typeInfo?.AsType();
#endif
        }
    }
}
