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

    internal class JsonSubtypesWithoutExplicitTypePropertyConverter : JsonSubtypes
    {
        private readonly Dictionary<Type, object> _supportedTypes;
        private JsonWriter _writer;
        private bool _isInsideWrite;
        private Type BaseType { get; }
        private Dictionary<object, Type> SubTypeMapping { get; }

        internal JsonSubtypesWithoutExplicitTypePropertyConverter(Type baseType, string discriminatorProperty, Dictionary<object, Type> subTypeMapping) : base(discriminatorProperty)
        {
            BaseType = baseType;
            SubTypeMapping = subTypeMapping;
            _supportedTypes = new Dictionary<Type, object>();
            foreach (var type in SubTypeMapping)
            {
                _supportedTypes.Add(type.Value, type.Key);
            }
        }

        protected override Dictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return SubTypeMapping;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == BaseType || _supportedTypes.ContainsKey(objectType);
        }

        public override bool CanWrite
        {
            get { return !_isInsideWrite; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _writer = writer;
            _isInsideWrite = true;
            try
            {
                var jo = JObject.FromObject(value, serializer);
                var supportedType = _supportedTypes[value.GetType()];
                var fromObject = JToken.FromObject(supportedType, serializer);
                jo.Add(_typeMappingPropertyName, fromObject);
                jo.WriteTo(writer);
            }
            finally
            {
                _isInsideWrite = false;
            }
        }
    }
}