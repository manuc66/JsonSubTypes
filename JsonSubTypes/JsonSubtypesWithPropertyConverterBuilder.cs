using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace JsonSubTypes
{
    public class JsonSubtypesWithPropertyConverterBuilder
    {
        private readonly Type _baseType;
        private readonly Dictionary<string, TypeWithPropertyMatchingAttributes> _subTypeMapping = new Dictionary<string, TypeWithPropertyMatchingAttributes>();
        private Type _fallbackSubtype;

        private JsonSubtypesWithPropertyConverterBuilder(Type baseType)
        {
            _baseType = baseType;
        }

        public static JsonSubtypesWithPropertyConverterBuilder Of(Type baseType)
        {
            return new JsonSubtypesWithPropertyConverterBuilder(baseType);
        }

        public static JsonSubtypesWithPropertyConverterBuilder Of<T>()
        {
            return Of(typeof(T));
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty(Type subtype, string jsonPropertyName, bool stopLookupOnMatch)
        {
            _subTypeMapping.Add(jsonPropertyName, new TypeWithPropertyMatchingAttributes(subtype, jsonPropertyName, stopLookupOnMatch));
            return this;
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty(Type subtype, string jsonPropertyName)
        {
            return RegisterSubtypeWithProperty(subtype, jsonPropertyName, false);
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty<T>(string jsonPropertyName)
        {
            return RegisterSubtypeWithProperty(typeof(T), jsonPropertyName);
        }

        public JsonSubtypesWithPropertyConverterBuilder SetFallbackSubtype(Type fallbackSubtype)
        {
            _fallbackSubtype = fallbackSubtype;
            return this;
        }

        public JsonSubtypesWithPropertyConverterBuilder SetFallbackSubtype<T>()
        {
            return SetFallbackSubtype(typeof(T));
        }

        public JsonConverter Build()
        {
            return new JsonSubtypesByPropertyPresenceConverter(_baseType, _subTypeMapping.Values.ToList(), _fallbackSubtype);
        }
    }
}
