using System;
using System.Collections.Concurrent;
using System.Text.Json;

namespace JsonSubTypes.Text.Json
{
    internal interface ISimpleMethod
    {
        object DeserializeSimple(ref Utf8JsonReader reader, JsonSerializerOptions options);
    }

    internal class DeserializerHelper<T> : ISimpleMethod
    {
        private static readonly ConcurrentDictionary<Type, ISimpleMethod> HelperCache =
            new ConcurrentDictionary<Type, ISimpleMethod>();

        private T Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options)!;
        }

        public object DeserializeSimple(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            return Deserialize(ref reader, options)!;
        }

        internal static T Deserialize(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions options)
        {
            ISimpleMethod genericConverterInstance = HelperCache.GetOrAdd(targetType, static type =>
                (ISimpleMethod)Activator.CreateInstance(typeof(DeserializerHelper<>).MakeGenericType(type))!);
            return (T)genericConverterInstance.DeserializeSimple(ref reader, options);
        }
    }
}
