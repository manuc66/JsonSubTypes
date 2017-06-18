using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonSubTypes
{
    public class JsonSubtypesWithNested : JsonSubtypes
    {
        public JsonSubtypesWithNested(string typeMappingPropertyName) : base(typeMappingPropertyName)
        {
        }

        public override bool CanRead
        {
            get
            {
                if (base.CanRead)
                    return true;

                var frame = new StackFrame(1);
                var method = frame.GetMethod();
                return method.Name == "SetPropertyValue";
            }
        }
    }
    public class JsonSubtypes : JsonConverter
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
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

        public override bool CanRead
        {
            get { return !_isInsideRead; }
        }

        public sealed override bool CanWrite
        {
            get { return false; }
        }

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

        public Type GetType(JObject jObject, Type type)
        {
            JToken jToken;
            if (!jObject.TryGetValue(_typeMappingPropertyName, out jToken)) return null;

            var objectType = jToken.ToObject<object>();
            if (objectType == null) return null;

            var typeMapping = type.GetCustomAttributes<KnownSubTypeAttribute>().ToDictionary(x => x.AssociatedValue, x => x.SubType);
            var lookupValue = Convert.ChangeType(objectType, typeMapping.First().Key.GetType());

            Type targetType;
            return typeMapping.TryGetValue(lookupValue, out targetType) ? targetType : null;
        }

        public override bool CanConvert(Type objectType)
        {
            return _typeMappingPropertyName != null && objectType.GetCustomAttributes<KnownSubTypeAttribute>().Any();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
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
