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
            public Type SubType { get; private set; }
            public object AssociatedValue { get; private set; }

            public KnownSubTypeAttribute(Type subType, object associatedValue)
            {
                SubType = subType;
                AssociatedValue = associatedValue;
            }
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
        public class KnownSubTypeWithPropertyAttribute : Attribute
        {
            public Type SubType { get; private set; }
            public string PropertyName { get; private set; }

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

        public override bool CanWrite
        {
            get { return false; }
        }

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
                    value = ReadArray(reader, objectType, serializer);
                    break;
                default:
                    var lineNumber = 0;
                    var linePosition = 0;
                    var lineInfo = reader as IJsonLineInfo;
                    if (lineInfo != null && lineInfo.HasLineInfo())
                    {
                        lineNumber = lineInfo.LineNumber;
                        linePosition = lineInfo.LinePosition;
                    }

                    throw new JsonReaderException(string.Format("Unrecognized token: {0}; path: {1}; lineNumber: {2}; linePosition: {3}", reader.TokenType, reader.Path, lineNumber, linePosition));
            }

            return value;
        }

        private IList ReadArray(JsonReader reader, Type targetType, JsonSerializer serializer)
        {
            var elementType = GetElementType(targetType);

            var list = CreateCompatibleList(targetType, elementType);
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                list.Add(ReadJson(reader, elementType, serializer));
            }

            if (!targetType.IsArray)
                return list;

            var array = Array.CreateInstance(targetType.GetElementType(), list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        private static IList CreateCompatibleList(Type targetContainerType, Type elementType)
        {
            var typeInfo = GetTypeInfo(targetContainerType);
            if (typeInfo.IsArray || typeInfo.IsAbstract)
            {
                return (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            }

            return (IList) Activator.CreateInstance(targetContainerType);
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
            var jObject = JObject.Load(reader);

            var targetType = GetType(jObject, objectType) ?? objectType;

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

        private Type GetType(JObject jObject, Type parentType)
        {
            if (JsonDiscriminatorPropertyName == null)
            {
                return GetTypeByPropertyPresence(jObject, parentType);
            }

            return GetTypeFromDiscriminatorValue(jObject, parentType);
        }

        private static Type GetTypeByPropertyPresence(IDictionary<string, JToken> jObject, Type parentType)
        {
            var knownSubTypeAttributes = GetAttributes<KnownSubTypeWithPropertyAttribute>(parentType);

            return knownSubTypeAttributes
                .Select(knownType =>
                {
                    JToken ignore;
                    if (TryGetValueInJson(jObject, knownType.PropertyName, out ignore))
                        return knownType.SubType;

                    return null;
                })
                .FirstOrDefault(type => type != null);
        }

        private Type GetTypeFromDiscriminatorValue(IDictionary<string, JToken> jObject, Type parentType)
        {
            JToken discriminatorValue;
            if (!TryGetValueInJson(jObject, JsonDiscriminatorPropertyName, out discriminatorValue))
                return null;

            if (discriminatorValue.Type == JTokenType.Null)
                return null;

            var typeMapping = GetSubTypeMapping(parentType);
            if (typeMapping.Any())
            {
                return GetTypeFromMapping(typeMapping, discriminatorValue);
            }

            return GetTypeByName(discriminatorValue.Value<string>(), parentType);
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

        private static Type GetTypeByName(string typeName, Type parentType)
        {
            if (typeName == null)
                return null;

            var insideAssembly = GetTypeInfo(parentType).Assembly;

            var typeByName = insideAssembly.GetType(typeName);
            if (typeByName != null)
                return typeByName;

            var searchLocation = parentType.FullName.Substring(0, parentType.FullName.Length - parentType.Name.Length);
            return insideAssembly.GetType(searchLocation + typeName, false, true);
        }

        private static Type GetTypeFromMapping(Dictionary<object, Type> typeMapping, JToken discriminatorToken)
        {
            var targetlookupValueType = typeMapping.First().Key.GetType();
            var lookupValue = discriminatorToken.ToObject(targetlookupValueType);

            Type targetType;
            if (typeMapping.TryGetValue(lookupValue, out targetType))
                return targetType;

            return null;
        }

        protected  virtual Dictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return GetAttributes<KnownSubTypeAttribute>(type)
                .ToDictionary(x => x.AssociatedValue, x => x.SubType);
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

        private static IEnumerable<T> GetAttributes<T>(Type type) where T : Attribute
        {
            return GetTypeInfo(type)
                .GetCustomAttributes(false)
                .OfType<T>();
        }

        private static IEnumerable<Type> GetGenericTypeArguments(Type type)
        {
#if (NET35 || NET40)
            var genericTypeArguments = type.GetGenericArguments();
#else
            var genericTypeArguments = type.GenericTypeArguments;
#endif
            return genericTypeArguments;
        }

        private static TypeInfo GetTypeInfo(Type type)
        {
#if (NET35 || NET40)
            return type;
#else
            return type.GetTypeInfo();
#endif
        }
    }
}