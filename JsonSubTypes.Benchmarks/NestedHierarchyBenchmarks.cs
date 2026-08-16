using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using JsonSubTypes.Text.Json;
using JsonSubTypes.Text.Json.Aot.Generated;
using StjBuilder = JsonSubTypes.Text.Json.JsonSubtypesConverterBuilder;

namespace JsonSubTypes.Benchmarks
{
    [MemoryDiagnoser]
    public class NestedHierarchyBenchmarks
    {
        private readonly JsonSerializerOptions? _converterOptions;
        private readonly JsonSerializerOptions _generatedOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = NestedContext.Default,
            Converters = { JsonSubTypesAotConverters.NestedPayload }
        };

        private readonly ConvRun _convRun = new ConvRun();
        private readonly NestedRun _nestedRun = new NestedRun();

        private readonly string? _converterJson;
        private readonly string _generatedJson;

        public NestedHierarchyBenchmarks()
        {
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                _converterOptions = new JsonSerializerOptions();
                _converterOptions.Converters.Add(StjBuilder
                    .Of(typeof(ConvPayload), "$PayloadKind")
                    .RegisterSubtype(typeof(ConvGame), PayloadDiscriminator.GAME)
                    .RegisterSubtype(typeof(ConvCom), PayloadDiscriminator.COM)
                    .Build());
                _converterOptions.Converters.Add(StjBuilder
                    .Of(typeof(ConvGame), "$GameKind")
                    .RegisterSubtype(typeof(ConvRun), GameDiscriminator.RUN)
                    .RegisterSubtype(typeof(ConvWalk), GameDiscriminator.WALK)
                    .Build());

                _converterJson = JsonSerializer.Serialize<ConvPayload>(_convRun, _converterOptions);
                BenchmarkValidation.DeserializeRoundTrips<ConvPayload, ConvRun>(_converterJson, _converterOptions);
            }

            _generatedJson = JsonSerializer.Serialize<NestedPayload>(_nestedRun, _generatedOptions);
            BenchmarkValidation.DeserializeRoundTrips<NestedPayload, NestedRun>(_generatedJson, _generatedOptions);
        }

        [Benchmark]
        public string Nested_Generated_Serialize() => JsonSerializer.Serialize<NestedPayload>(_nestedRun, _generatedOptions);

        [Benchmark]
        public ConvPayload? Nested_Converter_Deserialize() => JsonSerializer.Deserialize<ConvPayload>(_converterJson!, BenchmarkGuard.ReflectionOptions(_converterOptions));

        [Benchmark]
        public NestedPayload? Nested_Generated_Deserialize() => JsonSerializer.Deserialize<NestedPayload>(_generatedJson, _generatedOptions);
    }

    public enum PayloadDiscriminator
    {
        COM = 0,
        GAME = 1
    }

    public enum GameDiscriminator
    {
        RUN = 0,
        WALK = 1
    }

    public class ConvPayload
    {
        [JsonPropertyName("$PayloadKind")]
        public PayloadDiscriminator PayloadKind { get; set; } = PayloadDiscriminator.GAME;
    }

    public class ConvGame : ConvPayload
    {
        [JsonPropertyName("$GameKind")]
        public GameDiscriminator GameKind { get; set; } = GameDiscriminator.WALK;
    }

    public class ConvRun : ConvGame
    {
        public ConvRun()
        {
            PayloadKind = PayloadDiscriminator.GAME;
            GameKind = GameDiscriminator.RUN;
        }
    }

    public class ConvWalk : ConvGame
    {
    }

    public class ConvCom : ConvPayload
    {
    }

    [JsonSubTypesAotConverter("$PayloadKind")]
    [KnownSubType(typeof(NestedGame), PayloadDiscriminator.GAME)]
    [KnownSubType(typeof(NestedCom), PayloadDiscriminator.COM)]
    public class NestedPayload
    {
        [JsonPropertyName("$PayloadKind")]
        public PayloadDiscriminator PayloadKind { get; set; } = PayloadDiscriminator.GAME;
    }

    [JsonSubTypesAotConverter("$GameKind")]
    [KnownSubType(typeof(NestedRun), GameDiscriminator.RUN)]
    [KnownSubType(typeof(NestedWalk), GameDiscriminator.WALK)]
    public class NestedGame : NestedPayload
    {
        [JsonPropertyName("$GameKind")]
        public GameDiscriminator GameKind { get; set; } = GameDiscriminator.WALK;
    }

    public class NestedRun : NestedGame
    {
        public NestedRun()
        {
            PayloadKind = PayloadDiscriminator.GAME;
            GameKind = GameDiscriminator.RUN;
        }
    }

    public class NestedWalk : NestedGame
    {
    }

    public class NestedCom : NestedPayload
    {
    }

    [JsonSerializable(typeof(NestedPayload))]
    [JsonSerializable(typeof(NestedGame))]
    [JsonSerializable(typeof(NestedRun))]
    [JsonSerializable(typeof(NestedWalk))]
    [JsonSerializable(typeof(NestedCom))]
    public partial class NestedContext : JsonSerializerContext
    {
    }
}
