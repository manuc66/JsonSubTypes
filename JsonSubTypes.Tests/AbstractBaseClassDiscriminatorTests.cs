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
            var exception = Assert.Throws<JsonSerializationException>(() =>
                JsonConvert.DeserializeObject<MainClass>("{\"Discriminator\":\"MainClass\"}"));
            Assert.AreEqual(
                "No instantiable subtype has been found for: JsonSubTypes.Tests.AbstractBaseClassDiscriminatorTests+MainClass. Path '', line 1, position 1.",
                exception.Message);
        }

        [JsonConverter(typeof(JsonSubtypes), "Discriminator")]
        [JsonSubtypes.KnownSubType(typeof(B), "D")]
        public abstract class A
        {
        }

        [JsonConverter(typeof(JsonSubtypes), "Discriminator")]
        [JsonSubtypes.KnownSubType(typeof(C), "D")]
        public abstract class B
        {
        }

        [JsonConverter(typeof(JsonSubtypes), "Discriminator")]
        [JsonSubtypes.KnownSubType(typeof(A), "D")]
        public abstract class C
        {
        }


        [Test]
        [Timeout(2000)]
        public void DeserializingWithAbstractClassCircleThrows()
        {
            var exception = Assert.Throws<JsonSerializationException>(() =>
                JsonConvert.DeserializeObject<A>("{\"Discriminator\":\"D\"}"));
            Assert.AreEqual(
                "No instantiable subtype has been found for: JsonSubTypes.Tests.AbstractBaseClassDiscriminatorTests+A. Path '', line 1, position 1.",
                exception.Message);
        }
    }
}
