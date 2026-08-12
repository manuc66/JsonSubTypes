#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    /// <summary>
    /// Scenarios shared by the runtime-converter and generated-converter parity fixtures. Each
    /// engine only supplies its own <see cref="CreateOptions"/>, so a single set of assertions
    /// keeps both engines aligned. Engine-specific divergences live in the generated fixture.
    /// </summary>
    public abstract class EngineParityTests
    {
        protected abstract JsonSerializerOptions CreateOptions(bool caseInsensitive = false);

        [Test]
        public void SerializeTest()
        {
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
            var expected = new Root { Content = null };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":null}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeBadDocument()
        {
            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Root>("{\"Content\":8}", CreateOptions()));

            Assert.AreEqual("Unrecognized token: Number", exception?.Message);
        }

        [Test]
        public void WhenDiscriminatorValueIsNullDeserializeToBaseType()
        {
            var expected = new Root { Content = new Base() };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void WhenDiscriminatorValueIsUnknownDeserializeToBaseType()
        {
            var expected = new Root { Content = new Base() };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5}}", CreateOptions());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void WorkWithSubList()
        {
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
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<MainClass>("{\"Discriminator\":\"unknown\"}", CreateOptions()));

            StringAssert.Contains("abstract class and cannot be instantiated", exception?.Message);
        }

        [Test]
        public void InterfaceDeserialize()
        {
            var animal = JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}", CreateOptions());

            Assert.AreEqual("Jack Russell Terrier", (animal as PDog)?.Breed);
        }

        [Test]
        public void InterfaceUnknownMappingFails()
        {
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Scream\"}", CreateOptions()));

            StringAssert.Contains("interface", exception?.Message);
        }

        [Test]
        public void NestedLevelDeserialize()
        {
            const string data = "{\"$PayloadKind\":0,\"$GameKind\":0}";

            Assert.IsInstanceOf<Run>(JsonSerializer.Deserialize<Payload>(data, CreateOptions()));
        }

        [Test]
        public void MultiplePropertiesForSameSubtype()
        {
            var employee = JsonSerializer.Deserialize<MultiPropBase>("{\"JobTitle\":\"Dev\",\"FirstName\":\"A\"}", CreateOptions());

            Assert.IsInstanceOf<PEmployee>(employee);
        }

        [Test]
        public void FallbackReadWithParameterizedConstructor()
        {
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<ParameterizedBase>("{\"Kind\":\"unknown\"}", CreateOptions()));

            StringAssert.Contains("parameterless constructor", exception?.Message);
        }

        [Test]
        public void TypePropertyCaseInsensitive()
        {
            const string json = "{\"msgType\":1,\"MsgType\":1}";

            Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(json, CreateOptions(caseInsensitive: true)));
        }

        [Test]
        public void TypePropertyExactMatch()
        {
            const string json = "{\"msgType\":1,\"MsgType\":1}";

            Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(json, CreateOptions()));
        }
    }
}
