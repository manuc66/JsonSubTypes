using System;
using System.Collections.Generic;
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
        private readonly Dictionary<object, Type> _subTypeMapping;

        [ThreadStatic] private bool _isInsideWrite;
        [ThreadStatic] private bool _allowNextWrite;

        internal JsonSubtypesConverter(Type baseType, string discriminatorProperty, Dictionary<object, Type> subTypeMapping, bool serializeDiscriminatorProperty) : base(discriminatorProperty)
        {
            _serializeDiscriminatorProperty = serializeDiscriminatorProperty;
            _baseType = baseType;
            _subTypeMapping = subTypeMapping;
            foreach (var type in _subTypeMapping)
            {
                _supportedTypes.Add(type.Value, type.Key);
            }
        }

        protected override Dictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return _subTypeMapping;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == _baseType || _supportedTypes.ContainsKey(objectType);
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

            var supportedType = _supportedTypes[value.GetType()];
            var typeMappingPropertyValue = JToken.FromObject(supportedType, serializer);
            jsonObj.Add(_typeMappingPropertyName, typeMappingPropertyValue);

            jsonObj.WriteTo(writer);
        }
    }
}