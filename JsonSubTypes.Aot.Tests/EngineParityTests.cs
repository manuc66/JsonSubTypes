#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    [Flags]
    public enum ParityCapabilities
    {
        None = 0,
        ValueDiscriminator = 1,
        Presence = 2,
        NestedHierarchy = 4,
        DiscriminatorNameCollision = 8,
        BaseFallbackError = 16
    }

    /// <summary>
    /// Scenarios shared by the engine parity fixtures. Each scenario is written once and runs in
    /// every fixture whose <see cref="Capabilities"/> include it; scenarios an engine does not
    /// support are explicitly skipped with <see cref="Assert.Ignore"/>. The same scenarios run on
    /// every target framework of the test project.
    /// </summary>
    public abstract class EngineParityTests
    {
        protected abstract JsonSerializerOptions CreateOptions(bool caseInsensitive = false);

        protected abstract ParityCapabilities Capabilities { get; }

        private void Requires(ParityCapabilities capability)
        {
            if (!Capabilities.HasFlag(capability))
            {
                Assert.Ignore($"{capability} is not supported by this engine");
            }
        }

        [Test]
        public void SerializeTest()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var root = new Root
            {
                Content = new SubB
                {
                    Index = 1,
                    _4You = 2
                }
            };

            string str = JsonSerializer.Serialize(root, CreateOptions());

            Assert.AreEqual("{\"Content\":{\"@type\":\"SubB\",\"Index\":1,\"4-you\":2},\"ContentList\":null}", str);
        }

        [Test]
        public void SerializeThenDeserialize()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var root = new Root
            {
                Content = new SubB
                {
                    Index = 1,
                    _4You = 2
                }
            };

            string str = JsonSerializer.Serialize(root, CreateOptions());
            var back = JsonSerializer.Deserialize<Root>(str, CreateOptions());

            Assert.AreEqual(root, back);
        }

        [Test]
        public void DeserializeSubType()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":\"SubB\"}}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeSubTypeWithComments()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var options = CreateOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            var root = JsonSerializer.Deserialize<Root>(
                "{\"Content\":/* foo bar */{\"Index\":1,\"@type\":\"SubB\"}}", options);

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeNull()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var expected = new Root { Content = null };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":null}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeBadDocument()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Root>("{\"Content\":8}", CreateOptions()));

            Assert.AreEqual("Unrecognized token: Number", exception?.Message);
        }

        [Test]
        public void WhenDiscriminatorValueIsNullDeserializeToBaseType()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var expected = new Root { Content = new Base() };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void WhenDiscriminatorValueIsUnknownDeserializeToBaseType()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var expected = new Root { Content = new Base() };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5}}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void WorkWithSubList()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision);
            var expected = new Root
            {
                Content = new Base(),
                ContentList = new List<Base> { new SubB { Index = 1 }, new SubC { Name = "foo" } }
            };

            var root = JsonSerializer.Deserialize<Root>(
                "{\"Content\":{\"Index\":1,\"@type\":8.5},\"ContentList\":[{\"Index\":1,\"@type\":\"SubB\"},{\"Name\":\"foo\",\"@type\":\"SubC\"}]}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializingWithAbstractBaseClassDiscriminatorThrows()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.DiscriminatorNameCollision | ParityCapabilities.BaseFallbackError);
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<MainClass>("{\"Discriminator\":\"unknown\"}", CreateOptions()));

            StringAssert.Contains("abstract class and cannot be instantiated", exception?.Message);
        }

        [Test]
        public void InterfaceDeserialize()
        {
            Requires(ParityCapabilities.ValueDiscriminator);
            var animal = JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}", CreateOptions());

            Assert.AreEqual("Jack Russell Terrier", (animal as PDog)?.Breed);
        }

        [Test]
        public void InterfaceUnknownMappingFails()
        {
            Requires(ParityCapabilities.ValueDiscriminator);

            // the shared semantics: an unknown discriminator on an interface throws. The exact
            // message differs per engine (see the resolver divergence tests).
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Scream\"}", CreateOptions()));
        }

        [Test]
        public void NestedLevelDeserialize()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.NestedHierarchy);
            const string data = "{\"$PayloadKind\":0,\"$GameKind\":0}";

            Assert.IsInstanceOf<Run>(JsonSerializer.Deserialize<Payload>(data, CreateOptions()));
        }

        [Test]
        public void MultiplePropertiesForSameSubtype()
        {
            Requires(ParityCapabilities.Presence);
            var employee = JsonSerializer.Deserialize<MultiPropBase>("{\"JobTitle\":\"Dev\",\"FirstName\":\"A\"}", CreateOptions());

            Assert.IsInstanceOf<PEmployee>(employee);
        }

        [Test]
        public void FallbackReadWithParameterizedConstructor()
        {
            Requires(ParityCapabilities.ValueDiscriminator | ParityCapabilities.BaseFallbackError);
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<ParameterizedBase>("{\"Kind\":\"unknown\"}", CreateOptions()));

            StringAssert.Contains("parameterless constructor", exception?.Message);
        }

        [Test]
        public void TypePropertyCaseInsensitive()
        {
            Requires(ParityCapabilities.ValueDiscriminator);
            const string json = "{\"msgType\":1,\"MsgType\":1}";

            Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(json, CreateOptions(caseInsensitive: true)));
        }

        [Test]
        public void TypePropertyExactMatch()
        {
            Requires(ParityCapabilities.ValueDiscriminator);
            const string json = "{\"msgType\":1,\"MsgType\":1}";

            Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(json, CreateOptions()));
        }
    }
}
