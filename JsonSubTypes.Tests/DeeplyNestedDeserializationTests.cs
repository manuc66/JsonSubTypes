using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DeeplyNestedDeserializationTests
    {
        [JsonConverter(typeof(JsonSubtypes), nameof(SubTypeClass.Discriminator))]
        [JsonSubtypes.KnownSubType(typeof(SubTypeClass), "SubTypeClass")]
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

            var json = JsonConvert.SerializeObject(root);

            var obj = JsonConvert.DeserializeObject<MainClass>(json, new JsonSerializerSettings { MaxDepth = 65 });
            Assert.That(obj, Is.Not.Null);
        }
    }
}
