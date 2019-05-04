using System;
using System.Collections.Generic;

namespace JsonSubTypes
{
    internal class JsonSubtypesByPropertyPresenceConverter : JsonSubtypesConverter
    {
        private readonly Dictionary<string, Type> _jsonPropertyName2Type;

        internal JsonSubtypesByPropertyPresenceConverter(Type baseType, Dictionary<string, Type> jsonProperty2Type, Type fallbackType) : base(baseType, fallbackType)
        {
            _jsonPropertyName2Type = jsonProperty2Type;
        }

        internal override NullableDictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return new NullableDictionary<object, Type>();
        }

        internal override Dictionary<string, Type> GetTypesByPropertyPresence(Type parentType)
        {
            return _jsonPropertyName2Type;
        }
    }
}
