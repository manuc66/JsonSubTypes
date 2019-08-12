using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    internal class JsonSubtypesConverter : JsonSubtypes
    {
        private readonly bool _serializeDiscriminatorProperty;
        private readonly Dictionary<Type, object> _supportedTypes = new Dictionary<Type, object>();
        private readonly Type _baseType;
        private readonly NullableDictionary<object, Type> _subTypeMapping;

        [ThreadStatic] private static bool _isInsideWrite;
        [ThreadStatic] private static bool _allowNextWrite;
        private readonly bool _addDiscriminatorFirst;
        private readonly Type _fallbackType;

        internal JsonSubtypesConverter(Type baseType, string discriminatorProperty,
            NullableDictionary<object, Type> subTypeMapping, bool serializeDiscriminatorProperty, bool addDiscriminatorFirst, Type fallbackType) : base(discriminatorProperty)
        {
            _serializeDiscriminatorProperty = serializeDiscriminatorProperty;
            _baseType = baseType;
            _subTypeMapping = subTypeMapping;
            _addDiscriminatorFirst = addDiscriminatorFirst;
            _fallbackType = fallbackType;
            foreach (var type in subTypeMapping.Entries())
            {
                if (_supportedTypes.ContainsKey(type.Value))
                {
                    if (_serializeDiscriminatorProperty)
                    {
                        throw new InvalidOperationException(
                            "Multiple discriminators on single type are not supported " +
                            "when discriminator serialization is enabled");
                    }
                }
                else
                {
                    _supportedTypes.Add(type.Value, type.Key);
                }
            }
        }

        internal override NullableDictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return _subTypeMapping;
        }

        internal override Type GetFallbackSubType(Type type)
        {
            return _fallbackType;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == _baseType || _supportedTypes.ContainsKey(objectType) || ToTypeInfo(_baseType).IsAssignableFrom(ToTypeInfo(objectType));
        }

        public override bool CanWrite
        {
            get
            {
                if (!_serializeDiscriminatorProperty)
                    return false;

                if (!_isInsideWrite)
                    return true;

                if (_allowNextWrite)
                {
                    _allowNextWrite = false;
                    return true;
                }

                _allowNextWrite = true;
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jsonObj;
            _isInsideWrite = true;
            _allowNextWrite = false;
            try
            {
                jsonObj = JObject.FromObject(value, serializer);
            }
            finally
            {
                _isInsideWrite = false;
            }

            if (!_supportedTypes.TryGetValue(value.GetType(), out var supportedType))
            {
                throw new JsonSerializationException("Impossible to serialize type: " + value.GetType().FullName + " because there is no registered mapping for the discriminator property");
            }
            var typeMappingPropertyValue = JToken.FromObject(supportedType, serializer);
            if (_addDiscriminatorFirst)
            {
                jsonObj.AddFirst(new JProperty(JsonDiscriminatorPropertyName, typeMappingPropertyValue));
            }
            else
            {
                jsonObj.Add(JsonDiscriminatorPropertyName, typeMappingPropertyValue);
            }
            jsonObj.WriteTo(writer);
        }
    }
}
