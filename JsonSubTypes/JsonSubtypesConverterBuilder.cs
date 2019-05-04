using System;
using Newtonsoft.Json;

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

    public class JsonSubtypesConverterBuilder
    {
        private Type _baseType;
        private string _discriminatorProperty;
        private readonly NullableDictionary<object, Type> _subTypeMapping = new NullableDictionary<object, Type>();
        private bool _serializeDiscriminatorProperty;
        private bool _addDiscriminatorFirst;
        private Type _fallbackSubtype;

        public static JsonSubtypesConverterBuilder Of(Type baseType, string discriminatorProperty)
        {
            var customConverterBuilder = new JsonSubtypesConverterBuilder
            {
                _baseType = baseType,
                _discriminatorProperty = discriminatorProperty
            };
            return customConverterBuilder;
        }

        public JsonSubtypesConverterBuilder SerializeDiscriminatorProperty()
        {
            return SerializeDiscriminatorProperty(false);
        }

        public JsonSubtypesConverterBuilder SerializeDiscriminatorProperty(bool addDiscriminatorFirst)
        {
            _serializeDiscriminatorProperty = true;
            _addDiscriminatorFirst = addDiscriminatorFirst;
            return this;
        }

        public JsonSubtypesConverterBuilder RegisterSubtype(Type subtype, object value)
        {
            _subTypeMapping.Add(value, subtype);
            return this;
        }

        public JsonSubtypesConverterBuilder SetFallbackSubtype(Type fallbackSubtype)
        {
            _fallbackSubtype = fallbackSubtype;
            return this;
        }

        public JsonConverter Build()
        {
            return new JsonSubtypesConverter(_baseType, _discriminatorProperty, _subTypeMapping, _serializeDiscriminatorProperty, _addDiscriminatorFirst, _fallbackSubtype);
        }
    }
}
