using System;
using System.Text.Json;

namespace JsonSubTypes.Benchmarks
{
    // Every benchmark scenario must produce a correct result, otherwise we would be measuring a
    // broken path. Each benchmark constructor calls these helpers, which fail fast (throw) when
    // the scenario does not round-trip to the expected subtype with the expected discriminator.
    public static class BenchmarkValidation
    {
        public static string SerializeRoundTrips<T>(T value, string discriminatorName, string discriminatorValue,
            JsonSerializerOptions options)
        {
            string json = JsonSerializer.Serialize(value, options);
            if (!json.Contains($"\"{discriminatorName}\"") || !json.Contains(discriminatorValue))
            {
                throw new InvalidOperationException(
                    $"Serialize validation failed for {typeof(T).Name}: expected discriminator {discriminatorName}={discriminatorValue} in {json}");
            }
            return json;
        }

        public static void DeserializeRoundTrips<T, TSub>(string json, JsonSerializerOptions options)
            where TSub : T
        {
            T? result = JsonSerializer.Deserialize<T>(json, options);
            if (result is not TSub)
            {
                throw new InvalidOperationException(
                    $"Deserialize validation failed for {typeof(T).Name}: expected {typeof(TSub).Name} but got {(result == null ? "null" : result.GetType().Name)}");
            }
        }
    }
}
