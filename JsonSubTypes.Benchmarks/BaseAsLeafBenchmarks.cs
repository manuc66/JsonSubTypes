using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using JsonSubTypes.Text.Json;
using JsonSubTypes.Text.Json.Aot.Generated;
using StjBuilder = JsonSubTypes.Text.Json.JsonSubtypesConverterBuilder;

namespace JsonSubTypes.Benchmarks
{
    // Base-as-leaf: serializing/deserializing the polymorphic base type itself (not a subtype).
    // The converter uses its reflection-built base writer/reader on this path (see README), so
    // this measures that fallback machinery rather than the discriminator round-trip.
    [MemoryDiagnoser]
    public class BaseAsLeafBenchmarks
    {
        private readonly JsonSerializerOptions? _converterOptions;
        private readonly JsonSerializerOptions _generatedOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = BaseLeafContext.Default,
            Converters = { JsonSubTypesAotConverters.BaseLeafAnimal }
        };

        private readonly ConvBaseAnimal _convBase = new ConvBaseAnimal { Age = 3 };
        private readonly BaseLeafAnimal _generatedBase = new BaseLeafAnimal { Age = 3 };

        private readonly string? _converterJson;
        private readonly string _generatedJson;

        public BaseAsLeafBenchmarks()
        {
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                _converterOptions = new JsonSerializerOptions();
                _converterOptions.Converters.Add(StjBuilder.Of<ConvBaseAnimal>("type")
                    .RegisterSubtype<ConvBaseCat>("cat")
                    .RegisterSubtype<ConvBaseDog>("dog")
                    .Build());

                _converterJson = JsonSerializer.Serialize<ConvBaseAnimal>(_convBase, _converterOptions);
                BenchmarkValidation.DeserializeRoundTrips<ConvBaseAnimal, ConvBaseAnimal>(_converterJson, _converterOptions);
            }

            _generatedJson = JsonSerializer.Serialize<BaseLeafAnimal>(_generatedBase, _generatedOptions);
            BenchmarkValidation.DeserializeRoundTrips<BaseLeafAnimal, BaseLeafAnimal>(_generatedJson, _generatedOptions);
        }

        [Benchmark]
        public string Leaf_Converter_Serialize() => JsonSerializer.Serialize<ConvBaseAnimal>(_convBase, _converterOptions!);

        [Benchmark]
        public string Leaf_Generated_Serialize() => JsonSerializer.Serialize<BaseLeafAnimal>(_generatedBase, _generatedOptions);

        [Benchmark]
        public ConvBaseAnimal? Leaf_Converter_Deserialize() => JsonSerializer.Deserialize<ConvBaseAnimal>(_converterJson!, _converterOptions!);

        [Benchmark]
        public BaseLeafAnimal? Leaf_Generated_Deserialize() => JsonSerializer.Deserialize<BaseLeafAnimal>(_generatedJson, _generatedOptions);
    }

    public class ConvBaseAnimal { public int Age { get; set; } }
    public class ConvBaseCat : ConvBaseAnimal { public int Lives { get; set; } }
    public class ConvBaseDog : ConvBaseAnimal { public bool CanHunt { get; set; } }

    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(BaseLeafCat), "cat")]
    [KnownSubType(typeof(BaseLeafDog), "dog")]
    public class BaseLeafAnimal { public int Age { get; set; } }
    public class BaseLeafCat : BaseLeafAnimal { public int Lives { get; set; } }
    public class BaseLeafDog : BaseLeafAnimal { public bool CanHunt { get; set; } }

    [JsonSerializable(typeof(BaseLeafAnimal))]
    [JsonSerializable(typeof(BaseLeafCat))]
    [JsonSerializable(typeof(BaseLeafDog))]
    public partial class BaseLeafContext : JsonSerializerContext
    {
    }
}
