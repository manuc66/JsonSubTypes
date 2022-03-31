using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace NewApi;

internal class DeserializerHelper<T>
{
    //;
    private T Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }
    private delegate object DeserializeDelegate(ref Utf8JsonReader reader, JsonSerializerOptions options);

    public static T Deserialize(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions options)
    {
        Type ggg = typeof(DeserializerHelper<>);
        Type makeGenericType = ggg.MakeGenericType(targetType);
        object converter = Activator.CreateInstance(makeGenericType)!;

        var instance = Expression.Constant(converter);
        var method = converter.GetType().GetMethod(nameof(DeserializerHelper<object>.Deserialize),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var parameters = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();

        var call = Expression.Call(instance, method, parameters);
        var cast = Expression.TypeAs(call, typeof(object));

        var @delegate = Expression.Lambda<DeserializeDelegate>(cast, parameters);

        var result = @delegate.Compile()(ref reader, options);

        return (T)result;
    }
}

