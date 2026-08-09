using System;

namespace JsonSubTypes.Text.Json
{
    internal class TypeWithPropertyMatchingAttributes
    {
        public Type Type { get; }
        public string JsonPropertyName { get; }
        public bool StopLookupOnMatch { get; }

        public TypeWithPropertyMatchingAttributes(Type type, string jsonPropertyName, bool stopLookupOnMatch)
        {
            Type = type;
            JsonPropertyName = jsonPropertyName;
            StopLookupOnMatch = stopLookupOnMatch;
        }
    }
}
