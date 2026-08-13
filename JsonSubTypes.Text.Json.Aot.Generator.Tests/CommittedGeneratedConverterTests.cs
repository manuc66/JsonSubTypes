#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json.Aot.Generated;
using JsonSubTypes.Text.Json.Aot.Generated.TestDomain;
using NUnit.Framework;

namespace JsonSubTypes.Text.Json.Aot.Generator.Tests
{
    // Exercises the committed golden-master converters (the Generated/ files in
    // JsonSubTypes.Text.Json.Aot.Generated). Because those files are compiled as real sources,
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

        [Test]
        public void Payload_SerializeRun_WritesNestedChain()
        {
            string json = JsonSerializer.Serialize<Payload>(new Run(), PayloadAndGameOptions);

            Assert.That(json, Is.EqualTo("{\"$PayloadKind\":0,\"$GameKind\":0}"));
        }

        [Test]
        public void Payload_SerializeWalk_WritesNestedChain()
        {
            string json = JsonSerializer.Serialize<Payload>(new Walk(), PayloadAndGameOptions);

            Assert.That(json, Is.EqualTo("{\"$PayloadKind\":0,\"$GameKind\":1}"));
        }

        [Test]
        public void Payload_SerializeCom_WritesDirectDiscriminator()
        {
            string json = JsonSerializer.Serialize<Payload>(new Com(), PayloadOptions);

            Assert.That(json, Is.EqualTo("{\"$PayloadKind\":1}"));
        }

        [Test]
        public void Payload_DeserializeMissingDiscriminator_FallsBackToBase()
        {
            Payload? result = JsonSerializer.Deserialize<Payload>("{}", PayloadOptions);

            Assert.That(result, Is.InstanceOf<Payload>());
        }

        [Test]
        public void Payload_DeserializeUnknownValue_FallsBackToBase()
        {
            Payload? result = JsonSerializer.Deserialize<Payload>("{\"$PayloadKind\":\"nope\"}", PayloadOptions);

            Assert.That(result, Is.InstanceOf<Payload>());
        }

        [Test]
        public void Payload_DeserializeStringEnumNames_ReturnsSubtype()
        {
            Payload? game = JsonSerializer.Deserialize<Payload>("{\"$PayloadKind\":\"GAME\"}", PayloadOptions);
            Payload? com = JsonSerializer.Deserialize<Payload>("{\"$PayloadKind\":\"COM\"}", PayloadOptions);

            Assert.That(game, Is.InstanceOf<Game>());
            Assert.That(com, Is.InstanceOf<Com>());
        }

        [Test]
        public void Game_SerializeRun_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<Game>(new Run(), PayloadAndGameOptions);

            Assert.That(json, Is.EqualTo("{\"$GameKind\":0}"));
        }

        [Test]
        public void Game_DeserializeRun_ReturnsRun()
        {
            Game? result = JsonSerializer.Deserialize<Game>("{\"$GameKind\":0}", PayloadAndGameOptions);

            Assert.That(result, Is.InstanceOf<Run>());
        }

        [Test]
        public void Game_DeserializeWalk_ReturnsWalk()
        {
            Game? result = JsonSerializer.Deserialize<Game>("{\"$GameKind\":1}", PayloadAndGameOptions);

            Assert.That(result, Is.InstanceOf<Walk>());
        }

        [Test]
        public void Game_SerializeBase_WritesBaseObject()
        {
            string json = JsonSerializer.Serialize<Game>(new Game(), PayloadAndGameOptions);

            Assert.That(json, Is.EqualTo("{}"));
        }

        [Test]
        public void Game_DeserializeMissingDiscriminator_FallsBackToBase()
        {
            Game? result = JsonSerializer.Deserialize<Game>("{}", PayloadAndGameOptions);

            Assert.That(result, Is.InstanceOf<Game>());
        }

        [Test]
        public void Game_DeserializeUnknownValue_FallsBackToBase()
        {
            Game? result = JsonSerializer.Deserialize<Game>("{\"$GameKind\":\"nope\"}", PayloadAndGameOptions);

            Assert.That(result, Is.InstanceOf<Game>());
        }

        [Test]
        public void Game_DeserializeStringEnumNames_ReturnsSubtype()
        {
            Game? run = JsonSerializer.Deserialize<Game>("{\"$GameKind\":\"RUN\"}", PayloadAndGameOptions);
            Game? walk = JsonSerializer.Deserialize<Game>("{\"$GameKind\":\"WALK\"}", PayloadAndGameOptions);

            Assert.That(run, Is.InstanceOf<Run>());
            Assert.That(walk, Is.InstanceOf<Walk>());
        }

        [Test]
        public void Gadget_SerializeBase_WritesBaseObject()
        {
            string json = JsonSerializer.Serialize<Gadget>(new Gadget { Age = 3 }, Options<Gadget>());

            Assert.That(json, Is.EqualTo("{\"Age\":3}"));
        }

        [Test]
        public void Gadget_DeserializeUnknown_FallsBackToBase()
        {
            Gadget? result = JsonSerializer.Deserialize<Gadget>("{\"kind\":\"fish\",\"Age\":3}", Options<Gadget>());

            Assert.That(result, Is.InstanceOf<Gadget>());
        }

        [Test]
        public void Gadget_DeserializeStringEnumName_ReturnsSubtype()
        {
            Gadget? result = JsonSerializer.Deserialize<Gadget>("{\"kind\":\"ElectronicCat\",\"Lives\":9,\"Age\":3}", Options<Gadget>());

            Assert.That(result, Is.InstanceOf<ElectronicCat>());
        }

        [Test]
        public void DottedGadget_SerializeBase_WritesBaseObject()
        {
            string json = JsonSerializer.Serialize<DottedGadget>(new DottedGadget { Age = 3 }, Options<DottedGadget>());

            Assert.That(json, Is.EqualTo("{\"Age\":3}"));
        }

        [Test]
        public void DottedGadget_DeserializeUnknown_FallsBackToBase()
        {
            DottedGadget? result = JsonSerializer.Deserialize<DottedGadget>("{\"Age\":3}", Options<DottedGadget>());

            Assert.That(result, Is.InstanceOf<DottedGadget>());
        }

        [Test]
        public void DottedGadget_SerializeElectronic_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<DottedGadget>(new DottedElectronic { Age = 3, Lives = 9 }, Options<DottedGadget>());

            Assert.That(json, Is.EqualTo("{\"nested.type\":\"electronic\",\"Lives\":9,\"Age\":3}"));
        }

        [Test]
        public void Person_SerializeBase_WritesBaseObject()
        {
            string json = JsonSerializer.Serialize<Person>(new Person { FirstName = "A" }, Options<Person>());

            Assert.That(json, Is.EqualTo("{\"FirstName\":\"A\"}"));
        }

        [Test]
        public void DynamicShape_DeserializeCat_ReturnsCat()
        {
            DynamicShape? result = JsonSerializer.Deserialize<DynamicShape>("{\"kind\":\"cat\",\"Lives\":9,\"Age\":3}", Options<DynamicShape>());

            Assert.That(result, Is.InstanceOf<DynamicCat>());
        }

        // ---- edge paths on the shared skeleton ----

        [Test]
        public void Animal_SerializeNull_WritesNull()
        {
            string json = JsonSerializer.Serialize<Animal?>(null, Options<Animal>());

            Assert.That(json, Is.EqualTo("null"));
        }

        [Test]
        public void Animal_DeserializeNull_ReturnsNull()
        {
            Animal? result = JsonSerializer.Deserialize<Animal>("null", Options<Animal>());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Animal_DeserializeNonObject_Throws()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Animal>("[1,2]", Options<Animal>()));
        }

        [Test]
        public void Animal_SerializeBase_WritesBaseObjectWithoutDiscriminator()
        {
            string json = JsonSerializer.Serialize<Animal>(new Animal { Age = 3 }, Options<Animal>());

            Assert.That(json, Is.EqualTo("{\"Age\":3}"));
        }

        [Test]
        public void Animal_SerializeUnregisteredSubtype_WritesThroughResolver()
        {
            string json = JsonSerializer.Serialize<Animal>(new Owl { Wingspan = 40, Age = 3 }, Options<Animal>());

            Assert.That(json, Is.EqualTo("{\"Wingspan\":40,\"Age\":3}"));
        }

        // ---- null discriminator ----

        [Test]
        public void NullDiscriminatorAnimal_SerializeBase_WritesNullDiscriminator()
        {
            string json = JsonSerializer.Serialize<NullDiscriminatorAnimal>(new NullDiscriminatorAnimal { Age = 3 }, Options<NullDiscriminatorAnimal>());

            Assert.That(json, Is.EqualTo("{\"type\":null,\"Age\":3}"));
        }

        [Test]
        public void NullDiscriminatorAnimal_DeserializeNullDiscriminator_ReturnsBase()
        {
            NullDiscriminatorAnimal? result = JsonSerializer.Deserialize<NullDiscriminatorAnimal>("{\"type\":null,\"Age\":3}", Options<NullDiscriminatorAnimal>());

            Assert.That(result, Is.InstanceOf<NullDiscriminatorAnimal>());
        }

        [Test]
        public void NullDiscriminatorAnimal_SerializeDeer_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<NullDiscriminatorAnimal>(new Deer { Age = 3, AntlerSize = 5 }, Options<NullDiscriminatorAnimal>());

            Assert.That(json, Is.EqualTo("{\"type\":\"deer\",\"AntlerSize\":5,\"Age\":3}"));
        }

        [Test]
        public void NullDiscriminatorAnimal_DeserializeDeer_ReturnsDeer()
        {
            NullDiscriminatorAnimal? result = JsonSerializer.Deserialize<NullDiscriminatorAnimal>("{\"type\":\"deer\",\"AntlerSize\":5,\"Age\":3}", Options<NullDiscriminatorAnimal>());

            Assert.That(result, Is.InstanceOf<Deer>());
        }

        // ---- AddDiscriminatorFirst = false ----

        [Test]
        public void DiscriminatorLast_SerializeWritesDiscriminatorLast()
        {
            string json = JsonSerializer.Serialize<DiscriminatorLast>(new Mammoth { Age = 3, Tusks = 2 }, Options<DiscriminatorLast>());

            Assert.That(json, Is.EqualTo("{\"Tusks\":2,\"Age\":3,\"type\":\"mammoth\"}"));
        }

        [Test]
        public void DiscriminatorLast_DeserializeStillWorks()
        {
            DiscriminatorLast? result = JsonSerializer.Deserialize<DiscriminatorLast>("{\"type\":\"mammoth\",\"Tusks\":2,\"Age\":3}", Options<DiscriminatorLast>());

            Assert.That(result, Is.InstanceOf<Mammoth>());
        }

        [Test]
        public void DiscriminatorLast_SerializeBase_WritesBaseObject()
        {
            string json = JsonSerializer.Serialize<DiscriminatorLast>(new DiscriminatorLast { Age = 3 }, Options<DiscriminatorLast>());

            Assert.That(json, Is.EqualTo("{\"Age\":3}"));
        }

        [Test]
        public void DiscriminatorLast_DeserializeMissingDiscriminator_FallsBackToBase()
        {
            DiscriminatorLast? result = JsonSerializer.Deserialize<DiscriminatorLast>("{}", Options<DiscriminatorLast>());

            Assert.That(result, Is.InstanceOf<DiscriminatorLast>());
        }

        [Test]
        public void DiscriminatorLast_DeserializeUnknownValue_FallsBackToBase()
        {
            DiscriminatorLast? result = JsonSerializer.Deserialize<DiscriminatorLast>("{\"type\":\"nope\"}", Options<DiscriminatorLast>());

            Assert.That(result, Is.InstanceOf<DiscriminatorLast>());
        }

        // ---- get-only property and conditional JsonIgnore ----

        [Test]
        public void DynamicShape_SerializeBase_WritesGetterOnlyAndSkipsNullNickname()
        {
            string json = JsonSerializer.Serialize<DynamicShape>(new DynamicShape { Age = 3 }, Options<DynamicShape>());

            Assert.That(json, Is.EqualTo("{\"Age\":3,\"Computed\":\"computed\"}"));
        }

        [Test]
        public void DynamicShape_SerializeBase_WritesNicknameWhenNotNull()
        {
            string json = JsonSerializer.Serialize<DynamicShape>(new DynamicShape { Age = 3, Nickname = "N" }, Options<DynamicShape>());

            Assert.That(json, Is.EqualTo("{\"Age\":3,\"Computed\":\"computed\",\"Nickname\":\"N\"}"));
        }

        [Test]
        public void DynamicShape_DeserializeBase_PopulatesSettablePropertiesOnly()
        {
            DynamicShape? result = JsonSerializer.Deserialize<DynamicShape>("{\"Age\":3,\"Computed\":\"other\",\"Nickname\":\"N\"}", Options<DynamicShape>());

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Age, Is.EqualTo(3));
            Assert.That(result.Nickname, Is.EqualTo("N"));
            Assert.That(result.Computed, Is.EqualTo("computed"), "get-only property must not be overwritten on read");
        }

        [Test]
        public void DynamicShape_SerializeCat_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<DynamicShape>(new DynamicCat { Age = 3, Lives = 9 }, Options<DynamicShape>());

            Assert.That(json, Is.EqualTo("{\"kind\":\"cat\",\"Lives\":9,\"Age\":3,\"Computed\":\"computed\"}"));
        }

        // ---- dynamic subtype and custom resolver on the committed Animal converter ----

        [Test]
        public void Animal_SerializeDynamicSubtype_WritesDiscriminator()
        {
            JsonSubTypesAotConverters.Animal.RegisterDynamicSubtype("fish", typeof(Fox));

            try
            {
                string json = JsonSerializer.Serialize<Animal>(new Fox { Speed = 20, Age = 3 }, Options<Animal>());
                Assert.That(json, Is.EqualTo("{\"type\":\"fish\",\"Speed\":20,\"Age\":3}"));
            }
            finally
            {
                JsonSubTypesAotConverters.Animal.DynamicSubtypes.TryRemove("fish", out _);
            }
        }

        [Test]
        public void Animal_DeserializeDynamicDiscriminator_ReturnsDynamicType()
        {
            JsonSubTypesAotConverters.Animal.DynamicSubtypes["fish"] = typeof(Fox);

            try
            {
                Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":\"fish\",\"Speed\":20,\"Age\":3}", Options<Animal>());
                Assert.That(result, Is.InstanceOf<Fox>());
            }
            finally
            {
                JsonSubTypesAotConverters.Animal.DynamicSubtypes.TryRemove("fish", out _);
            }
        }

        [Test]
        public void Animal_DeserializeCustomResolver_ResolvesArbitraryName()
        {
            JsonSubTypesAotConverters.Animal.CustomTypeNameResolver = name =>
                name as string == "bird" ? typeof(Fox) : null;

            try
            {
                Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":\"bird\",\"Speed\":20,\"Age\":3}", Options<Animal>());
                Assert.That(result, Is.InstanceOf<Fox>());
            }
            finally
            {
                JsonSubTypesAotConverters.Animal.CustomTypeNameResolver = null;
            }
        }
    }
}
