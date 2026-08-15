using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace JsonSubTypes.Benchmarks
{
    // Newtonsoft.Json baseline: the original package. These benchmarks mirror the single-object
    // and collection scenarios of the System.Text.Json benchmarks so the two packages can be
    // compared. Newtonsoft runs on reflection only, so these benchmarks report NA under the
    // NativeAOT job (like the reflection-based STJ engines).
    [MemoryDiagnoser]
    public class NewtonsoftBenchmarks
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings();

        private readonly NwAnimal _animal = new NwCat { Age = 3, Lives = 9 };
        private readonly List<NwAnimal> _animals;

        private readonly string _singleJson;
        private readonly string _collectionJson;

        public NewtonsoftBenchmarks()
        {
            _settings.Converters.Add(JsonSubTypes.JsonSubtypesConverterBuilder
                .Of<NwAnimal>("type")
                .RegisterSubtype<NwCat>("cat")
                .RegisterSubtype<NwDog>("dog")
                .SerializeDiscriminatorProperty()
                .Build());

            _animals = new List<NwAnimal>
            {
                new NwCat { Age = 3, Lives = 9 },
                new NwDog { Age = 5, CanHunt = true },
                new NwCat { Age = 7, Lives = 7 },
                new NwDog { Age = 1, CanHunt = false }
            };

            _singleJson = JsonConvert.SerializeObject(_animal, _settings);
            _collectionJson = JsonConvert.SerializeObject(_animals, _settings);

            if (JsonConvert.DeserializeObject<NwAnimal>(_singleJson, _settings) is not NwCat)
            {
                throw new InvalidOperationException("Newtonsoft single round-trip validation failed");
            }
            if (JsonConvert.DeserializeObject<List<NwAnimal>>(_collectionJson, _settings) is not List<NwAnimal> { Count: 4 })
            {
                throw new InvalidOperationException("Newtonsoft collection round-trip validation failed");
            }
        }

        [Benchmark]
        public string Nw_Single_Serialize() => JsonConvert.SerializeObject(_animal, _settings);

        [Benchmark]
        public NwAnimal? Nw_Single_Deserialize() => JsonConvert.DeserializeObject<NwAnimal>(_singleJson, _settings);

        [Benchmark]
        public string Nw_Collection_Serialize() => JsonConvert.SerializeObject(_animals, _settings);

        [Benchmark]
        public List<NwAnimal>? Nw_Collection_Deserialize() => JsonConvert.DeserializeObject<List<NwAnimal>>(_collectionJson, _settings);
    }

    public class NwAnimal { public int Age { get; set; } }
    public class NwCat : NwAnimal { public int Lives { get; set; } }
    public class NwDog : NwAnimal { public bool CanHunt { get; set; } }
}
