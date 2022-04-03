using System;
using System.Text.Json;

namespace JsonSubTypes.Text.Json;
internal interface ISimpleMethod
{
    object DeserializeSimple(ref Utf8JsonReader reader, JsonSerializerOptions options);
}
internal class DeserializerHelper<T> : ISimpleMethod
{
    private T Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public object DeserializeSimple(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return Deserialize(ref reader, options);
    }
    
    internal static T Deserialize(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions options)
    {
        Type converterTargetType = typeof(DeserializerHelper<>).MakeGenericType(targetType);
        ISimpleMethod genericConverterInstance = (ISimpleMethod)Activator.CreateInstance(converterTargetType)!;
        return (T)genericConverterInstance.DeserializeSimple(ref reader, options);
    }
}

