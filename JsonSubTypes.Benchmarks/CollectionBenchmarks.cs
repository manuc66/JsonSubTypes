using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using JsonSubTypes.Text.Json;
using JsonSubTypes.Text.Json.Aot.Generated;
using StjBuilder = JsonSubTypes.Text.Json.JsonSubtypesConverterBuilder;

namespace JsonSubTypes.Benchmarks
{
    [MemoryDiagnoser]
    public class CollectionBenchmarks
    {
        private readonly JsonSerializerOptions? _converterOptions;
        private readonly JsonSerializerOptions? _resolverOptions;
        private readonly JsonSerializerOptions _generatedOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = ColContext.Default,
            Converters = { JsonSubTypesAotConverters.ColAnimal }
        };

        private readonly List<ColConvAnimal> _convAnimals;
        private readonly List<ColResAnimal> _resAnimals;
        private readonly List<ColAnimal> _generatedAnimals;

        private readonly string? _converterJson;
        private readonly string? _resolverJson;
        private readonly string _generatedJson;

        public CollectionBenchmarks()
        {
            _convAnimals = new List<ColConvAnimal>
            {
                new ColConvCat { Age = 3, Lives = 9 },
                new ColConvDog { Age = 5, CanHunt = true },
                new ColConvCat { Age = 7, Lives = 7 },
                new ColConvDog { Age = 1, CanHunt = false }
            };
            _resAnimals = new List<ColResAnimal>
            {
                new ColResCat { Age = 3, Lives = 9 },
                new ColResDog { Age = 5, CanHunt = true },
                new ColResCat { Age = 7, Lives = 7 },
                new ColResDog { Age = 1, CanHunt = false }
            };
            _generatedAnimals = new List<ColAnimal>
            {
                new ColCat { Age = 3, Lives = 9 },
                new ColDog { Age = 5, CanHunt = true },
                new ColCat { Age = 7, Lives = 7 },
                new ColDog { Age = 1, CanHunt = false }
            };

            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                _converterOptions = new JsonSerializerOptions();
                _converterOptions.Converters.Add(StjBuilder.Of<ColConvAnimal>("type")
                    .RegisterSubtype<ColConvCat>("cat")
                    .RegisterSubtype<ColConvDog>("dog")
                    .SerializeDiscriminatorProperty()
                    .Build());
                _resolverOptions = new JsonSerializerOptions
                {
                    TypeInfoResolver = StjBuilder.Of<ColResAnimal>("type")
                        .RegisterSubtype<ColResCat>("cat")
                        .RegisterSubtype<ColResDog>("dog")
                        .SerializeDiscriminatorProperty()
                        .BuildResolver()
                };

                _converterJson = JsonSerializer.Serialize(_convAnimals, _converterOptions);
                BenchmarkValidation.DeserializeRoundTrips<List<ColConvAnimal>, List<ColConvAnimal>>(_converterJson, _converterOptions);
                _resolverJson = JsonSerializer.Serialize(_resAnimals, _resolverOptions);
                BenchmarkValidation.DeserializeRoundTrips<List<ColResAnimal>, List<ColResAnimal>>(_resolverJson, _resolverOptions);
            }

            _generatedJson = JsonSerializer.Serialize(_generatedAnimals, _generatedOptions);
            BenchmarkValidation.DeserializeRoundTrips<List<ColAnimal>, List<ColAnimal>>(_generatedJson, _generatedOptions);
        }

        [Benchmark]
        public string Col_Converter_Serialize() => JsonSerializer.Serialize(_convAnimals, _converterOptions!);

        [Benchmark]
        public string Col_Resolver_Serialize() => JsonSerializer.Serialize(_resAnimals, _resolverOptions!);

        [Benchmark]
        public string Col_Generated_Serialize() => JsonSerializer.Serialize(_generatedAnimals, _generatedOptions);

        [Benchmark]
        public List<ColConvAnimal>? Col_Converter_Deserialize() => JsonSerializer.Deserialize<List<ColConvAnimal>>(_converterJson!, _converterOptions!);

        [Benchmark]
        public List<ColResAnimal>? Col_Resolver_Deserialize() => JsonSerializer.Deserialize<List<ColResAnimal>>(_resolverJson!, _resolverOptions!);

        [Benchmark]
        public List<ColAnimal>? Col_Generated_Deserialize() => JsonSerializer.Deserialize<List<ColAnimal>>(_generatedJson, _generatedOptions);
    }

    public class ColConvAnimal { public int Age { get; set; } }
    public class ColConvCat : ColConvAnimal { public int Lives { get; set; } }
    public class ColConvDog : ColConvAnimal { public bool CanHunt { get; set; } }

    public class ColResAnimal { public int Age { get; set; } }
    public class ColResCat : ColResAnimal { public int Lives { get; set; } }
    public class ColResDog : ColResAnimal { public bool CanHunt { get; set; } }

    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(ColCat), "cat")]
    [KnownSubType(typeof(ColDog), "dog")]
    public class ColAnimal { public int Age { get; set; } }
    public class ColCat : ColAnimal { public int Lives { get; set; } }
    public class ColDog : ColAnimal { public bool CanHunt { get; set; } }

    [JsonSerializable(typeof(ColAnimal))]
    [JsonSerializable(typeof(ColCat))]
    [JsonSerializable(typeof(ColDog))]
    [JsonSerializable(typeof(List<ColAnimal>))]
    public partial class ColContext : JsonSerializerContext
    {
    }
}
