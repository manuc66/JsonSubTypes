using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace JsonSubTypes.Benchmarks
{
    // Newtonsoft's native $type baseline (TypeNameHandling.Auto), the same single-object and
    // collection scenarios as NewtonsoftBenchmarks, so the discriminator converter can be compared
    // against Newtonsoft's built-in type-name handling. Auto writes $type only on members whose
    // declared type is abstract/interface/object (never on a root declared as the base type), so
    // the single scenario goes through a holder and the base type is abstract here.
    [MemoryDiagnoser]
    public class NewtonsoftTypeNameHandlingBenchmarks
    {
        private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        private readonly TnHolder _holder = new TnHolder { Animal = new TnCat { Age = 3, Lives = 9 } };
        private readonly List<TnAnimal> _animals;

        private readonly string _singleJson;
        private readonly string _collectionJson;

        public NewtonsoftTypeNameHandlingBenchmarks()
        {
            _animals = new List<TnAnimal>
            {
                new TnCat { Age = 3, Lives = 9 },
                new TnDog { Age = 5, CanHunt = true },
                new TnCat { Age = 7, Lives = 7 },
                new TnDog { Age = 1, CanHunt = false }
            };

            _singleJson = JsonConvert.SerializeObject(_holder, _settings);
            _collectionJson = JsonConvert.SerializeObject(_animals, _settings);

            if (JsonConvert.DeserializeObject<TnHolder>(_singleJson, _settings)?.Animal is not TnCat)
            {
                throw new InvalidOperationException("TypeNameHandling single round-trip validation failed");
            }
            if (JsonConvert.DeserializeObject<List<TnAnimal>>(_collectionJson, _settings) is not List<TnAnimal> { Count: 4 })
            {
                throw new InvalidOperationException("TypeNameHandling collection round-trip validation failed");
            }
        }

        [Benchmark]
        public string Single_Serialize() => JsonConvert.SerializeObject(_holder, _settings);

        [Benchmark]
        public TnHolder? Single_Deserialize() => JsonConvert.DeserializeObject<TnHolder>(_singleJson, _settings);

        [Benchmark]
        public string Collection_Serialize() => JsonConvert.SerializeObject(_animals, _settings);

        [Benchmark]
        public List<TnAnimal>? Collection_Deserialize() => JsonConvert.DeserializeObject<List<TnAnimal>>(_collectionJson, _settings);
    }

    public abstract class TnAnimal { public int Age { get; set; } }
    public class TnCat : TnAnimal { public int Lives { get; set; } }
    public class TnDog : TnAnimal { public bool CanHunt { get; set; } }
    public class TnHolder { public TnAnimal? Animal { get; set; } }
}
