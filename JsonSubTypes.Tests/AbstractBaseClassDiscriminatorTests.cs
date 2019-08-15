using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class AbstractBaseClassDiscriminatorTests
    {
        [JsonConverter(typeof(JsonSubtypes), nameof(SubTypeClass.Discriminator))]
        public abstract class MainClass
        {
        }

        public class SubTypeClass : MainClass
        {
            public string Discriminator => "SubTypeClass";
        }

        [Test]
        [Timeout(2000)]
        public void DeserializingWithAbstractBaseClassDiscriminatorThrows()
        {
            Assert.Throws<JsonSerializationException>(() =>
                JsonConvert.DeserializeObject<MainClass>("{\"Discriminator\":\"MainClass\"}"));
        }
    }
}
