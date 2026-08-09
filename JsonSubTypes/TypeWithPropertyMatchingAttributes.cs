using System;

namespace JsonSubTypes
{
    internal class TypeWithPropertyMatchingAttributes
    {
        internal Type Type { get; }
        internal string JsonPropertyName { get; }
        internal bool StopLookupOnMatch { get; }

        public TypeWithPropertyMatchingAttributes(Type type, string jsonPropertyName, bool stopLookupOnMatch)
        {
            Type = type;
            JsonPropertyName = jsonPropertyName;
            StopLookupOnMatch = stopLookupOnMatch;
        }
    }
}
