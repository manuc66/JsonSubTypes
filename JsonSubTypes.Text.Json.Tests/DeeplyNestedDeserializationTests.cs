using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DeeplyNestedDeserializationTests
    {
        [JsonSubTypeConverter(typeof(JsonSubtypes<MainClass>), nameof(SubTypeClass.Discriminator))]
        [KnownSubType(typeof(SubTypeClass), "SubTypeClass")]
        public abstract class MainClass
        {
        }

        public class SubTypeClass : MainClass
        {
            public string Discriminator => "SubTypeClass";

            public MainClass Child { get; set; }
        }

        [Test]
        public void DeserializingDeeplyNestedJsonWithHighMaxDepthParsesCorrectly()
        {
            var root = new SubTypeClass();

            var current = root;
            for (var i = 0; i < 64; i++)
            {
                var child = new SubTypeClass();
                current.Child = child;
                current = child;
            }

            var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { MaxDepth = 128 });

            var obj = JsonSerializer.Deserialize<MainClass>(json, new JsonSerializerOptions { MaxDepth = 128 });
            Assert.That(obj, Is.Not.Null);
        }
    }
}
