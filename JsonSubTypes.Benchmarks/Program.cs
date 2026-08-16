using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.NativeAot;
using JsonSubTypes.Text.Json.Aot.Generated;
using JsonSubTypes.Text.Json;
using StjBuilder = JsonSubTypes.Text.Json.JsonSubtypesConverterBuilder;

namespace JsonSubTypes.Benchmarks
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IConfig config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.Default)
                .AddJob(Job.Default
                    .WithToolchain(NativeAotToolchain.Net10_0)
                    .WithId("NativeAOT"));

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }

    [MemoryDiagnoser]
    public class PolymorphismBenchmarks
    {
        private readonly JsonSerializerOptions? _converterOptions;
        private readonly JsonSerializerOptions? _resolverOptions;
        private readonly JsonSerializerOptions _generatedOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = BenchContext.Default,
            Converters = { JsonSubTypesAotConverters.BenchAnimal }
        };

        private readonly ResCat _resCat = new ResCat { Age = 3, Lives = 9 };
        private readonly BenchCat _benchCat = new BenchCat { Age = 3, Lives = 9 };

        private readonly string? _converterJson;
        private readonly string? _resolverJson;
        private readonly string _generatedJson;

        public PolymorphismBenchmarks()
        {
            // the converter/resolver options rely on reflection and only exist when it is enabled
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                _converterOptions = new JsonSerializerOptions();
                _converterOptions.Converters.Add(StjBuilder.Of<ConvAnimal>("type")
                    .RegisterSubtype<ConvCat>("cat")
                    .RegisterSubtype<ConvDog>("dog")
                    .SerializeDiscriminatorProperty()
                    .Build());
                _resolverOptions = new JsonSerializerOptions
                {
                    TypeInfoResolver = StjBuilder.Of<ResAnimal>("type")
                        .RegisterSubtype<ResCat>("cat")
                        .RegisterSubtype<ResDog>("dog")
                        .SerializeDiscriminatorProperty()
                        .BuildResolver()
                };

                _converterJson = BenchmarkValidation.SerializeRoundTrips<ConvAnimal>(new ConvCat { Age = 3, Lives = 9 },
                    "type", "\"cat\"", _converterOptions);
                BenchmarkValidation.DeserializeRoundTrips<ConvAnimal, ConvCat>(_converterJson, _converterOptions);

                _resolverJson = BenchmarkValidation.SerializeRoundTrips<ResAnimal>(new ResCat { Age = 3, Lives = 9 },
                    "type", "\"cat\"", _resolverOptions);
                BenchmarkValidation.DeserializeRoundTrips<ResAnimal, ResCat>(_resolverJson, _resolverOptions);
            }

            _generatedJson = BenchmarkValidation.SerializeRoundTrips<BenchAnimal>(new BenchCat { Age = 3, Lives = 9 },
                "type", "\"cat\"", _generatedOptions);
            BenchmarkValidation.DeserializeRoundTrips<BenchAnimal, BenchCat>(_generatedJson, _generatedOptions);
        }

        [Benchmark]
        public string Generated_Serialize() => JsonSerializer.Serialize<BenchAnimal>(_benchCat, _generatedOptions);

        [Benchmark]
        public string Resolver_Serialize() => JsonSerializer.Serialize<ResAnimal>(_resCat, BenchmarkGuard.ReflectionOptions(_resolverOptions));

        [Benchmark]
        public string Converter_Serialize() => JsonSerializer.Serialize<ConvAnimal>(new ConvCat { Age = 3, Lives = 9 }, BenchmarkGuard.ReflectionOptions(_converterOptions));

        [Benchmark]
        public ConvAnimal? Converter_Deserialize() => JsonSerializer.Deserialize<ConvAnimal>(_converterJson!, BenchmarkGuard.ReflectionOptions(_converterOptions));

        [Benchmark]
        public ResAnimal? Resolver_Deserialize() => JsonSerializer.Deserialize<ResAnimal>(_resolverJson!, BenchmarkGuard.ReflectionOptions(_resolverOptions));

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

    [JsonSerializable(typeof(BenchAnimal))]
    [JsonSerializable(typeof(BenchCat))]
    [JsonSerializable(typeof(BenchDog))]
    public partial class BenchContext : JsonSerializerContext
    {
    }
}
