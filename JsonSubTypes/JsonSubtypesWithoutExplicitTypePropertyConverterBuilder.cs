using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JsonSubTypes
{
    public class JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
    {
        private Type _baseType;
        private string _discriminatorProperty;
        private readonly Dictionary<object, Type> _subTypeMapping = new Dictionary<object, Type>();

        public static JsonSubtypesWithoutExplicitTypePropertyConverterBuilder Of(Type baseType, string discriminatorProperty)
        {
            var customConverterBuilder = new JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
            {
                _baseType = baseType,
                _discriminatorProperty = discriminatorProperty
            };
            return customConverterBuilder;
        }

        public JsonSubtypesWithoutExplicitTypePropertyConverterBuilder RegisterSubtype(Type subtype, object value)
        {
            _subTypeMapping.Add(value, subtype);
            return this;
        }

        public JsonConverter Build()
        {
            return new JsonSubtypesWithoutExplicitTypePropertyConverter(_baseType, _discriminatorProperty, _subTypeMapping);
        }
    }
}