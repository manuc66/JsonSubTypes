#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Aot.Generated;
using JsonSubTypes.Aot.Generated.TestDomain;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Generator.Tests
{
    // Exercises the committed golden-master converters (the Generated/ files in
    // JsonSubTypes.Aot.Generated). Because those files are compiled as real sources,
    // coverlet measures them here and Sonar analyzes them. Every converter must be
    // exercised on both serialize and deserialize so its Write/Read/SelectType paths
    // are covered.
    [TestFixture]
    public class CommittedGeneratedConverterTests
    {
        private static JsonSerializerOptions Options<T>()
        {
            return new JsonSerializerOptions { Converters = { GetConverter<T>() } };
        }

        private static JsonConverter GetConverter<T>()
        {
            // resolve the converter instance from the committed registry
            return (JsonConverter)RegistryConverter(typeof(T));
        }

        private static object RegistryConverter(System.Type baseType)
        {
            var registry = typeof(JsonSubTypesAotConverters);
            string memberName = baseType.Name;
            return registry.GetField(memberName)?.GetValue(null)
                ?? throw new System.InvalidOperationException("No committed converter for " + baseType.Name);
        }

        // ---- Animal (string + int discriminators) ----

        [Test]
        public void Animal_SerializeCat_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<Animal>(new Cat { Age = 3, Lives = 9 }, Options<Animal>());
            Assert.That(json, Does.Contain("\"type\":\"cat\""));
        }

        [Test]
        public void Animal_DeserializeCatDiscriminator_ReturnsCat()
        {
            Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":\"cat\",\"Lives\":9,\"Age\":3}", Options<Animal>());
            Assert.That(result, Is.InstanceOf<Cat>());
        }

        [Test]
        public void Animal_DeserializeIntDogDiscriminator_ReturnsDog()
        {
            Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":2,\"CanHunt\":true,\"Age\":3}", Options<Animal>());
            Assert.That(result, Is.InstanceOf<Dog>());
        }

        [Test]
        public void Animal_DeserializeUnknownDiscriminator_FallsBackToBase()
        {
            Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":\"fish\",\"Age\":3}", Options<Animal>());
            Assert.That(result, Is.InstanceOf<Animal>());
            Assert.That(result, Is.Not.InstanceOf<Cat>());
        }

        // ---- Person (property presence + fallback) ----

        [Test]
        public void Person_SerializeArtist_NoDiscriminatorWritten()
        {
            string json = JsonSerializer.Serialize<Person>(new Artist { Skill = "Painter", FirstName = "A" }, Options<Person>());
            Assert.That(json, Does.Contain("\"Skill\""));
        }

        [Test]
        public void Person_DeserializeBySkill_ReturnsArtist()
        {
            Person? result = JsonSerializer.Deserialize<Person>("{\"Skill\":\"Painter\",\"FirstName\":\"A\"}", Options<Person>());
            Assert.That(result, Is.InstanceOf<Artist>());
        }

        [Test]
        public void Person_DeserializeUnknown_FallsBackToBase()
        {
            Person? result = JsonSerializer.Deserialize<Person>("{\"FirstName\":\"A\"}", Options<Person>());
            Assert.That(result, Is.InstanceOf<Person>());
            Assert.That(result, Is.Not.InstanceOf<Artist>());
        }

        // ---- Gadget (enum discriminator) ----

        [Test]
        public void Gadget_SerializeElectronicCat_WritesEnumDiscriminator()
        {
            string json = JsonSerializer.Serialize<Gadget>(new ElectronicCat { Age = 3, Lives = 9 }, Options<Gadget>());
            Assert.That(json, Does.Contain("\"kind\":"));
        }

        [Test]
        public void Gadget_DeserializeElectronicCat_ReturnsSubtype()
        {
            Gadget? result = JsonSerializer.Deserialize<Gadget>("{\"kind\":0,\"Lives\":9,\"Age\":3}", Options<Gadget>());
            Assert.That(result, Is.InstanceOf<ElectronicCat>());
        }

        // ---- DottedGadget (nested discriminator path) ----

        [Test]
        public void DottedGadget_DeserializeNestedDiscriminator_ReturnsSubtype()
        {
            DottedGadget? result = JsonSerializer.Deserialize<DottedGadget>(
                "{\"nested\":{\"type\":\"electronic\"},\"Lives\":9,\"Age\":3}", Options<DottedGadget>());
            Assert.That(result, Is.InstanceOf<DottedElectronic>());
        }

        // ---- Payload / Game (nested multi-level hierarchy) ----

        private static readonly JsonSerializerOptions PayloadAndGameOptions = new()
        {
            TypeInfoResolver = TestDomainJsonContext.Default,
            Converters =
            {
                (JsonConverter)RegistryConverter(typeof(Payload)),
                (JsonConverter)RegistryConverter(typeof(Game))
            }
        };

        private static readonly JsonSerializerOptions PayloadOptions = new()
        {
            TypeInfoResolver = TestDomainJsonContext.Default,
            Converters = { (JsonConverter)RegistryConverter(typeof(Payload)) }
        };

        [Test]
        public void Payload_DeserializeNestedGameKind_ReturnsRun()
        {
            Payload? result = JsonSerializer.Deserialize<Payload>(
                "{\"$PayloadKind\":0,\"$GameKind\":0}", PayloadAndGameOptions);
            Assert.That(result, Is.InstanceOf<Run>());
        }

        [Test]
        public void Payload_DeserializeCom_ReturnsCom()
        {
            Payload? result = JsonSerializer.Deserialize<Payload>(
                "{\"$PayloadKind\":1}", PayloadOptions);
            Assert.That(result, Is.InstanceOf<Com>());
        }
    }
}
