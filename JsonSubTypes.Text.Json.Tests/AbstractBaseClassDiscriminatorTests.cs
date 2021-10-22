using System.Text.Json;
using JsonSubTypes.Text.Json;
using NewApi;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class AbstractBaseClassDiscriminatorTests
    {
        [JsonSubTypeConverter(typeof(JsonSubtypes<MainClass>), nameof(SubTypeClass.Discriminator))]
        public abstract class MainClass
        {
        }

        public class SubTypeClass : MainClass
        {
            public string Discriminator => "SubTypeClass";
        }

        [Test]
        [Timeout(2000)]
        [Ignore("Not ready")]
        public void DeserializingWithAbstractBaseClassDiscriminatorThrows()
        {
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<MainClass>("{\"Discriminator\":\"MainClass\"}"));
            Assert.AreEqual(
                "Could not create an instance of type JsonSubTypes.Tests.AbstractBaseClassDiscriminatorTests+MainClass. Type is an interface or abstract class and cannot be instantiated. Path 'Discriminator', line 1, position 17.",
                exception.Message);
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<A>), "Discriminator")]
        [KnownSubType(typeof(B), "D")]
        public abstract class A
        {
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<B>), "Discriminator")]
        [KnownSubType(typeof(C), "D")]
        public abstract class B
        {
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<C>), "Discriminator")]
        [KnownSubType(typeof(A), "D")]
        public abstract class C
        {
        }


        [Test]
        [Timeout(2000)]
        public void DeserializingWithAbstractClassCircleThrows()
        {
            Assert.Fail("Not ready");
            var exception = Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<A>("{\"Discriminator\":\"D\"}"));
            Assert.AreEqual(
                "Could not create an instance of type JsonSubTypes.Tests.AbstractBaseClassDiscriminatorTests+A. Type is an interface or abstract class and cannot be instantiated. Path 'Discriminator', line 1, position 17.",
                exception.Message);
        }
    }
}
