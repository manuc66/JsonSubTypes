using System;
using System.Collections.Generic;

namespace JsonSubTypes
{
    internal class JsonSubtypesByPropertyPresenceConverter : JsonSubtypesConverter
    {
        private readonly List<TypeWithPropertyMatchingAttributes> _jsonPropertyName2Type;

        internal JsonSubtypesByPropertyPresenceConverter(Type baseType, List<TypeWithPropertyMatchingAttributes> jsonProperty2Type, Type fallbackType) : base(baseType, fallbackType)
        {
            _jsonPropertyName2Type = jsonProperty2Type;
        }

        internal override List<TypeWithPropertyMatchingAttributes> GetTypesByPropertyPresence(Type parentType)
        {
            return _jsonPropertyName2Type;
        }
    }
}
