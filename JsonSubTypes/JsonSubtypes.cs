using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonSubTypes
{
    //  Copyright 2017 Emmanuel Counasse
    //  
    //  Licensed under the Apache License, Version 2.0 (the "License");
    //  you may not use this file except in compliance with the License.
    //  You may obtain a copy of the License at [apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0)

    //  Unless required by applicable law or agreed to in writing, software
    //  distributed under the License is distributed on an "AS IS" BASIS,
    //  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    //  See the License for the specific language governing permissions and
    //  limitations under the License.

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

        private readonly string _typeMappingPropertyName;

        private bool _isInsideRead;
        private JsonReader _reader;

        public override bool CanRead
        {
            get
            {
                if (!_isInsideRead)
                    return true;

                return !string.IsNullOrEmpty(_reader.Path);
            }
        }

        public sealed override bool CanWrite
        {
            get { return false; }
        }

        public JsonSubtypes(string typeMappingPropertyName)
        {
            _typeMappingPropertyName = typeMappingPropertyName;
        }

        public override bool CanConvert(Type objectType)
        {
            return _typeMappingPropertyName != null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartArray:
                    return ReadArray(reader, objectType, serializer);
                case JsonToken.StartObject:
                    return ReadObject(reader, objectType, serializer);
                default:
                    throw new Exception("Array: Unrecognized token: " + reader.TokenType);
            }
        }

        private IList ReadArray(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            var elementType = objectType.IsArray ? objectType.GetElementType() : objectType.GenericTypeArguments[0];
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            while (reader.TokenType != JsonToken.EndArray && reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.Null:
                        list.Add(reader.Value);
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.StartObject:
                        list.Add(ReadObject(reader, elementType, serializer));
                        break;
                    case JsonToken.EndArray:
                        break;
                    default:
                        throw new Exception("Array: Unrecognized token: " + reader.TokenType);
                }
            }
            if (objectType.IsArray)
            {
                var array = Array.CreateInstance(objectType.GetElementType(), list.Count);
                list.CopyTo(array, 0);
                list = array;
            }
            return list;
        }

        private object ReadObject(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var targetType = GetType(jObject, objectType) ?? objectType;

            return _ReadJson(jObject.CreateReader(), targetType, null, serializer);
        }

        public Type GetType(JObject jObject, Type type)
        {
            JToken jToken;
            if (!jObject.TryGetValue(_typeMappingPropertyName, out jToken)) return null;

            var discriminatorValue = jToken.ToObject<object>();
            if (discriminatorValue == null) return null;

            var typeMapping = GetSubTypeMapping(type);
            if (typeMapping.Any())
            {
                return GetTypeFromMapping(typeMapping, discriminatorValue);
            }

            return GetTypeByName(type, discriminatorValue);
        }

        private static Type GetTypeByName(Type type, object discriminatorValue)
        {
            var typeName = discriminatorValue as string;
            return typeName != null ? type.Assembly.GetType(typeName) : null;
        }

        private static Type GetTypeFromMapping(IReadOnlyDictionary<object, Type> typeMapping, object discriminatorValue)
        {
            var targetlookupValueType = typeMapping.First().Key.GetType();
            var lookupValue = ConvertJsonValueToType(discriminatorValue, targetlookupValueType);

            Type targetType;
            return typeMapping.TryGetValue(lookupValue, out targetType) ? targetType : null;
        }

        private static Dictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return type.GetCustomAttributes<KnownSubTypeAttribute>().ToDictionary(x => x.AssociatedValue, x => x.SubType);
        }

        private static object ConvertJsonValueToType(object objectType, Type targetlookupValueType)
        {
            if (targetlookupValueType.IsEnum)
                return Enum.ToObject(targetlookupValueType, objectType);

            return Convert.ChangeType(objectType, targetlookupValueType);
        }

        protected object _ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            _reader = reader;
            _isInsideRead = true;
            try
            {
                return serializer.Deserialize(reader, objectType);
            }
            finally
            {
                _isInsideRead = false;
            }
        }
    }
}
