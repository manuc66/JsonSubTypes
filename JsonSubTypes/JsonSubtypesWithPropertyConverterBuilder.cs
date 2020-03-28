using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace JsonSubTypes
{
    public class JsonSubtypesWithPropertyConverterBuilder
    {
        private readonly Type _baseType;
        private readonly Dictionary<string, Type> _subTypeMapping = new Dictionary<string, Type>();
        private Type _fallbackSubtype;

        private JsonSubtypesWithPropertyConverterBuilder(Type baseType)
        {
            _baseType = baseType;
        }

        public static JsonSubtypesWithPropertyConverterBuilder Of(Type baseType)
        {
            return new JsonSubtypesWithPropertyConverterBuilder(baseType);
        }

        public JsonSubtypesWithPropertyConverterBuilder RegisterSubtypeWithProperty(Type subtype, string jsonPropertyName)
        {
            _subTypeMapping.Add(jsonPropertyName, subtype);
            return this;
        }

        public JsonSubtypesWithPropertyConverterBuilder SetFallbackSubtype(Type fallbackSubtype)
        {
            _fallbackSubtype = fallbackSubtype;
            return this;
        }

        public JsonConverter Build()
        {
            return new JsonSubtypesByPropertyPresenceConverter(_baseType, _subTypeMapping, _fallbackSubtype);
        }
    }
}
