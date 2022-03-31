using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace JsonSubTypes.Text.Json;

internal class DeserializerHelper<T>
{
    private T Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }
    private delegate object DeserializeDelegate(ref Utf8JsonReader reader, JsonSerializerOptions options);

    public static T Deserialize(ref Utf8JsonReader reader, Type targetType, JsonSerializerOptions options)
    {
        Type converterTargetType = typeof(DeserializerHelper<>).MakeGenericType(targetType);
        object genericConverterInstance = Activator.CreateInstance(converterTargetType)!;

        ConstantExpression instance = Expression.Constant(genericConverterInstance);
        MethodInfo method = genericConverterInstance.GetType().GetMethod(nameof(DeserializerHelper<object>.Deserialize),
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        ParameterExpression[] parameters = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name))
            .ToArray();
        
        MethodCallExpression call = Expression.Call(instance, method, parameters);
        UnaryExpression cast = Expression.TypeAs(call, typeof(object));

        Expression<DeserializeDelegate> @delegate = Expression.Lambda<DeserializeDelegate>(cast, parameters);

        object result = @delegate.Compile()(ref reader, options);

        return (T)result;
    }
}

