using System;
using System.Text.Json;

namespace JsonSubTypes.Benchmarks
{
    // The converter, resolver and Newtonsoft benchmarks rely on reflection, which the
    // Native AOT job disables. Route their options through these guards so those benchmarks
    // fail loudly under the Native AOT job instead of silently running against the default
    // options (which would produce plausible-looking numbers that measure nothing).
    internal static class BenchmarkGuard
    {
        public static JsonSerializerOptions ReflectionOptions(JsonSerializerOptions? options)
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                throw new NotSupportedException(
                    "This engine relies on reflection, which the Native AOT host disables; the benchmark is not measured under that job.");
            }

            return options ?? throw new NotSupportedException("Benchmark options were not initialized.");
        }

        public static void RequireReflection()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                throw new NotSupportedException(
                    "This engine relies on reflection, which the Native AOT host disables; the benchmark is not measured under that job.");
            }
        }
    }
}
