using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonSubTypes
{
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