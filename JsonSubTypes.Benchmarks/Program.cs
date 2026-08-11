using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using JsonSubTypes.Aot.Generated;
using JsonSubTypes.Text.Json;

namespace JsonSubTypes.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }

    [MemoryDiagnoser]
    public class PolymorphismBenchmarks
    {
        private readonly JsonSerializerOptions _converterOptions = new JsonSerializerOptions();
        private readonly JsonSerializerOptions _resolverOptions;
        private readonly JsonSerializerOptions _generatedOptions = new JsonSerializerOptions
        {
            Converters = { JsonSubTypesAotConverters.BenchAnimal }
        };

        private readonly ResCat _resCat = new ResCat { Age = 3, Lives = 9 };
        private readonly BenchCat _benchCat = new BenchCat { Age = 3, Lives = 9 };

        private readonly string _converterJson;
        private readonly string _resolverJson;
        private readonly string _generatedJson;

        public PolymorphismBenchmarks()
        {
            _converterOptions.Converters.Add(JsonSubtypesConverterBuilder.Of<ConvAnimal>("type")
                .RegisterSubtype<ConvCat>("cat")
                .RegisterSubtype<ConvDog>("dog")
                .SerializeDiscriminatorProperty()
                .Build());
            _resolverOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<ResAnimal>("type")
                    .RegisterSubtype<ResCat>("cat")
                    .RegisterSubtype<ResDog>("dog")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };

            _converterJson = JsonSerializer.Serialize<ConvAnimal>(new ConvCat { Age = 3, Lives = 9 }, _converterOptions);
            _resolverJson = JsonSerializer.Serialize<ResAnimal>(new ResCat { Age = 3, Lives = 9 }, _resolverOptions);
            _generatedJson = JsonSerializer.Serialize<BenchAnimal>(new BenchCat { Age = 3, Lives = 9 }, _generatedOptions);
        }

        [Benchmark]
        public string Converter_Serialize() => JsonSerializer.Serialize<ConvAnimal>(new ConvCat { Age = 3, Lives = 9 }, _converterOptions);

        [Benchmark]
        public string Resolver_Serialize() => JsonSerializer.Serialize<ResAnimal>(_resCat, _resolverOptions);

        [Benchmark]
        public string Generated_Serialize() => JsonSerializer.Serialize<BenchAnimal>(_benchCat, _generatedOptions);

        [Benchmark]
        public ConvAnimal? Converter_Deserialize() => JsonSerializer.Deserialize<ConvAnimal>(_converterJson, _converterOptions);

        [Benchmark]
        public ResAnimal? Resolver_Deserialize() => JsonSerializer.Deserialize<ResAnimal>(_resolverJson, _resolverOptions);

        [Benchmark]
        public BenchAnimal? Generated_Deserialize() => JsonSerializer.Deserialize<BenchAnimal>(_generatedJson, _generatedOptions);
    }

    public class ConvAnimal { public int Age { get; set; } }
    public class ConvCat : ConvAnimal { public int Lives { get; set; } }
    public class ConvDog : ConvAnimal { public bool CanHunt { get; set; } }

    public class ResAnimal { public int Age { get; set; } }
    public class ResCat : ResAnimal { public int Lives { get; set; } }
    public class ResDog : ResAnimal { public bool CanHunt { get; set; } }

    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(BenchCat), "cat")]
    [KnownSubType(typeof(BenchDog), "dog")]
    public class BenchAnimal { public int Age { get; set; } }
    public class BenchCat : BenchAnimal { public int Lives { get; set; } }
    public class BenchDog : BenchAnimal { public bool CanHunt { get; set; } }
}
