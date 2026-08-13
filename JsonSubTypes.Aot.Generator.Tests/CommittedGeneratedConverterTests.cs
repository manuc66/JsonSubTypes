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
    // coverlet measures them here and Sonar analyzes them.
    [TestFixture]
    public class CommittedGeneratedConverterTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions
            {
                Converters = { JsonSubTypesAotConverters.Animal }
            };
        }

        [Test]
        public void Serialize_Cat_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<Animal>(new Cat { Age = 3, Lives = 9 }, Options());

            Assert.That(json, Does.Contain("\"type\":\"cat\""));
        }

        [Test]
        public void Deserialize_CatDiscriminator_ReturnsCat()
        {
            Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":\"cat\",\"Lives\":9,\"Age\":3}", Options());

            Assert.That(result, Is.InstanceOf<Cat>());
        }

        [Test]
        public void Deserialize_UnknownDiscriminator_FallsBackToBase()
        {
            Animal? result = JsonSerializer.Deserialize<Animal>("{\"type\":\"fish\",\"Age\":3}", Options());

            Assert.That(result, Is.InstanceOf<Animal>());
            Assert.That(result, Is.Not.InstanceOf<Cat>());
        }
    }
}
