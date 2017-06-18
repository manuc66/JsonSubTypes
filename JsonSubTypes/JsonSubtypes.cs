using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonSubTypes
{
    public class JsonSubtypes : JsonConverter
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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

        private readonly string _typeMappingPropertyName;

        private bool _isInsideRead;
        private bool _isInsideWrite;

        public sealed override bool CanRead => !_isInsideRead;
        public sealed override bool CanWrite => !_isInsideWrite;

        public JsonSubtypes(string typeMappingPropertyName)
        {
            _typeMappingPropertyName = typeMappingPropertyName;
        }

        protected object _ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
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

        protected void _WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _isInsideWrite = true;
            try
            {
                serializer.Serialize(writer, value);
            }
            finally
            {
                _isInsideWrite = false;
            }
        }

        public Type GetType(JObject jObject, Type type)
        {
            var typeMapping = type.GetCustomAttributes<KnownSubTypeAttribute>().ToDictionary(x => x.AssociatedValue?.ToString() ?? "null", x => x.SubType);

            var objectType = jObject[_typeMappingPropertyName].Value<string>();
            if (objectType != null && typeMapping.ContainsKey(objectType))
            {
                return typeMapping[objectType];
            }
            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return _typeMappingPropertyName != null && objectType.GetCustomAttributes<KnownSubTypeAttribute>().Any();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _WriteJson(writer, value, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jObject = JObject.Load(reader);

            var targetType = GetType(jObject, objectType) ?? objectType;

            return _ReadJson(jObject.CreateReader(), targetType, null, serializer);
        }
    }
}
