using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using JsonSubTypes.Text.Json;
using JsonSubTypes.Text.Json.Aot.Generated;
using StjWithPropertyBuilder = JsonSubTypes.Text.Json.JsonSubtypesWithPropertyConverterBuilder;

namespace JsonSubTypes.Benchmarks
{
    [MemoryDiagnoser]
    public class PropertyPresenceBenchmarks
    {
        private readonly JsonSerializerOptions? _converterOptions;
        private readonly JsonSerializerOptions _generatedOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = PresenceContext.Default,
            Converters = { JsonSubTypesAotConverters.PresencePerson }
        };

        private readonly ConvEmployee _convEmployee = new ConvEmployee { JobTitle = "Dev" };
        private readonly PresenceEmployee _presenceEmployee = new PresenceEmployee { JobTitle = "Dev" };

        private readonly string? _converterJson;
        private readonly string _generatedJson;

        public PropertyPresenceBenchmarks()
        {
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                _converterOptions = new JsonSerializerOptions();
                _converterOptions.Converters.Add(StjWithPropertyBuilder
                    .Of(typeof(ConvPerson))
                    .RegisterSubtypeWithProperty<ConvEmployee>("JobTitle")
                    .RegisterSubtypeWithProperty<ConvArtist>("Skill")
                    .Build());

                _converterJson = JsonSerializer.Serialize<ConvPerson>(_convEmployee, _converterOptions);
                BenchmarkValidation.DeserializeRoundTrips<ConvPerson, ConvEmployee>(_converterJson, _converterOptions);
            }

            _generatedJson = JsonSerializer.Serialize<PresencePerson>(_presenceEmployee, _generatedOptions);
            BenchmarkValidation.DeserializeRoundTrips<PresencePerson, PresenceEmployee>(_generatedJson, _generatedOptions);
        }

        [Benchmark]
        public string Pres_Converter_Serialize() => JsonSerializer.Serialize<ConvPerson>(_convEmployee, _converterOptions!);

        [Benchmark]
        public string Pres_Generated_Serialize() => JsonSerializer.Serialize<PresencePerson>(_presenceEmployee, _generatedOptions);

        [Benchmark]
        public ConvPerson? Pres_Converter_Deserialize() => JsonSerializer.Deserialize<ConvPerson>(_converterJson!, _converterOptions!);

        [Benchmark]
        public PresencePerson? Pres_Generated_Deserialize() => JsonSerializer.Deserialize<PresencePerson>(_generatedJson, _generatedOptions);
    }

    public class ConvPerson
    {
        public string? FirstName { get; set; }
    }

    public class ConvEmployee : ConvPerson
    {
        public string? JobTitle { get; set; }
    }

    public class ConvArtist : ConvPerson
    {
        public string? Skill { get; set; }
    }

    [JsonSubTypesAotConverter]
    [KnownSubTypeWithProperty(typeof(PresenceEmployee), "JobTitle")]
    [KnownSubTypeWithProperty(typeof(PresenceArtist), "Skill")]
    public class PresencePerson
    {
        public string? FirstName { get; set; }
    }

    public class PresenceEmployee : PresencePerson
    {
        public string? JobTitle { get; set; }
    }

    public class PresenceArtist : PresencePerson
    {
        public string? Skill { get; set; }
    }

    [JsonSerializable(typeof(PresencePerson))]
    [JsonSerializable(typeof(PresenceEmployee))]
    [JsonSerializable(typeof(PresenceArtist))]
    public partial class PresenceContext : JsonSerializerContext
    {
    }
}
