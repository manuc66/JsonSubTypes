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
                "Could not create an instance of type JsonSubTypes.Tests.AbstractBaseClassDiscriminatorTests+MainClass. Type is an interface or abstract class and cannot be instantiated. Path 'Discriminator', line 1, position 17.",
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
                "Could not create an instance of type JsonSubTypes.Tests.AbstractBaseClassDiscriminatorTests+A. Type is an interface or abstract class and cannot be instantiated. Path 'Discriminator', line 1, position 17.",
                exception.Message);
        }
    }

    [TestFixture]
    public class KnownBaseType_AbstractBaseClassDiscriminatorTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Discriminator")]
        [JsonSubtypes.KnownBaseType(typeof(CC), "D")]
        public abstract class AA
        {
        }

        [JsonConverter(typeof(JsonSubtypes), "Discriminator")]
        [JsonSubtypes.KnownBaseType(typeof(AA), "D")]
        public abstract class BB
        {
        }

        [JsonConverter(typeof(JsonSubtypes), "Discriminator")]
        [JsonSubtypes.KnownBaseType(typeof(BB), "D")]
        public abstract class CC
        {
        }

        [Test]
        [Timeout(2000)]
        public void DeserializingWithAbstractClassCircleThrows()
        {
            var exception = Assert.Throws<JsonSerializationException>(() =>
                JsonConvert.DeserializeObject<AA>("{\"Discriminator\":\"D\"}"));
            Assert.AreEqual(
                "Could not create an instance of type JsonSubTypes.Tests.KnownBaseType_AbstractBaseClassDiscriminatorTests+AA. Type is an interface or abstract class and cannot be instantiated. Path 'Discriminator', line 1, position 17.",
                exception.Message);
        }
    }
}
