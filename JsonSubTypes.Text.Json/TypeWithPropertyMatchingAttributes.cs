using System;

namespace JsonSubTypes.Text.Json;

internal class TypeWithPropertyMatchingAttributes(Type type, string jsonPropertyName, bool stopLookupOnMatch)
{
    public Type Type { get; } = type;
    public string JsonPropertyName { get; } = jsonPropertyName;
    public bool StopLookupOnMatch { get; } = stopLookupOnMatch;
}
