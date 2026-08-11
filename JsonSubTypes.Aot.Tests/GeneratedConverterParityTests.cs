#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Aot.Generated;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    [TestFixture]
    public class GeneratedParityRootTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.Base } };
        }

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

            string str = JsonSerializer.Serialize(root, Options());

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

            string str = JsonSerializer.Serialize(root, Options());
            var back = JsonSerializer.Deserialize<Root>(str, Options());

            Assert.AreEqual(root, back);
        }

        [Test]
        public void DeserializeSubType()
        {
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":\"SubB\"}}", Options());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeSubTypeWithComments()
        {
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var options = Options();
            options.ReadCommentHandling = JsonCommentHandling.Skip;
            var root = JsonSerializer.Deserialize<Root>(
                "{\"Content\":/* foo bar */{\"Index\":1,\"@type\":\"SubB\"}}", options);

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeNull()
        {
            var expected = new Root { Content = null };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":null}", Options());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeBadDocument()
        {
            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Root>("{\"Content\":8}", Options()));

            Assert.AreEqual("Unrecognized token: Number", exception?.Message);
        }

        [Test]
        public void WhenDiscriminatorValueIsNullDeserializeToBaseType()
        {
            var expected = new Root { Content = new Base() };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}", Options());

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void WhenDiscriminatorValueIsUnknownDeserializeToBaseType()
        {
            var expected = new Root { Content = new Base() };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5}}", Options());

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
                "{\"Content\":{\"Index\":1,\"@type\":8.5},\"ContentList\":[{\"Index\":1,\"@type\":\"SubB\"},{\"Name\":\"foo\",\"@type\":\"SubC\"}]}", Options());

            Assert.AreEqual(expected, root);
        }
    }

    [TestFixture]
    public class GeneratedParityAbstractBaseTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.MainClass } };
        }

        [Test]
        public void DeserializingWithAbstractBaseClassDiscriminatorThrows()
        {
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<MainClass>("{\"Discriminator\":\"unknown\"}", Options()));

            StringAssert.Contains("abstract class and cannot be instantiated", exception?.Message);
        }
    }

    [TestFixture]
    public class GeneratedParityInterfaceBaseTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.IAnimal } };
        }

        [Test]
        public void Test()
        {
            var animal = JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}", Options());

            Assert.AreEqual("Jack Russell Terrier", (animal as PDog)?.Breed);
        }

        [Test]
        public void UnknownMappingFails()
        {
            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Scream\"}", Options()));

            StringAssert.Contains("interface", exception?.Message);
        }
    }

    [TestFixture]
    public class GeneratedParityNestedHierarchyTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions
            {
                Converters = { JsonSubTypesAotConverters.Payload, JsonSubTypesAotConverters.Game }
            };
        }

        [Test]
        public void ShouldDeserializeNestedLevel()
        {
            const string data = "{\"$PayloadKind\":0,\"$GameKind\":0}";

            Assert.IsInstanceOf<Run>(JsonSerializer.Deserialize<Payload>(data, Options()));
        }

        [Test]
        public void ShouldSerializeNestedLevel()
        {
            string data = JsonSerializer.Serialize<Payload>(new Run(), Options());

            Assert.AreEqual("{\"$PayloadKind\":0,\"$GameKind\":0}", data);
        }
    }

    [TestFixture]
    public class GeneratedParityReviewBugTests
    {
        [Test]
        public void FallbackWriteRespectsJsonIgnore()
        {
            var options = new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.PAnimal } };

            string json = JsonSerializer.Serialize<PAnimal>(new PAnimal { Name = "Rex" }, options);

            Assert.That(json, Does.Not.Contain("Secret"));
        }

        [Test]
        public void MultiplePropertiesForSameSubtype()
        {
            var options = new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.MultiPropBase } };

            var employee = JsonSerializer.Deserialize<MultiPropBase>("{\"JobTitle\":\"Dev\",\"FirstName\":\"A\"}", options);

            Assert.IsInstanceOf<PEmployee>(employee);
        }

        [Test]
        public void FallbackReadWithParameterizedConstructor()
        {
            var options = new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.ParameterizedBase } };

            var exception = Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<ParameterizedBase>("{\"Kind\":\"unknown\"}", options));

            StringAssert.Contains("parameterless constructor", exception?.Message);
        }
    }

    [TestFixture]
    public class GeneratedParityTypePropertyCaseTests
    {
        [Test]
        public void FooParsingCamelCase()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { JsonSubTypesAotConverters.DtoBase }
            };

            string serializeObject = "{\"msgType\":1,\"MsgType\":1}";

            Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject, options));
        }

        [Test]
        public void FooParsingLowerPascalCase()
        {
            string serializeObject = "{\"msgType\":1,\"MsgType\":1}";

            Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject,
                new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.DtoBase } }));
        }
    }

    // ---- domain types ----

    public class Root
    {
        public Base? Content { get; set; }
        public List<Base>? ContentList { get; set; }

        protected bool Equals(Root other)
        {
            if (Equals(Content, other.Content))
            {
                return ContentList == null || other.ContentList == null
                    ? ReferenceEquals(ContentList, other.ContentList)
                    : ContentList.SequenceEqual(other.ContentList);
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Root)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Content != null ? Content.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ContentList != null
                               ? ContentList.Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode())
                               : 0);
                return hashCode;
            }
        }
    }

    [JsonSubTypesAotConverter("@type")]
    [KnownSubType(typeof(SubB), "SubB")]
    [KnownSubType(typeof(SubC), "SubC")]
    public class Base
    {
        [JsonPropertyName("@type")]
        public virtual string Type => "";

        [JsonPropertyName("4-you")]
        public int _4You { get; set; }

        protected bool Equals(Base other) => string.Equals(Type, other.Type) && _4You == other._4You;
        public override bool Equals(object? obj) => obj is Base b && Equals(b);
        public override int GetHashCode() => (Type.GetHashCode() * 397) ^ _4You;
    }

    public class SubB : Base
    {
        [JsonPropertyName("@type")]
        public override string Type => "SubB";

        public int Index { get; set; }

        protected bool Equals(SubB other) => base.Equals(other) && Index == other.Index;
        public override bool Equals(object? obj) => obj is SubB b && Equals(b);
        public override int GetHashCode() => (base.GetHashCode() * 397) ^ Index;
    }

    public class SubC : Base
    {
        [JsonPropertyName("@type")]
        public override string Type => "SubC";

        public string? Name { get; set; }

        protected bool Equals(SubC other) => base.Equals(other) && string.Equals(Name, other.Name);
        public override bool Equals(object? obj) => obj is SubC c && Equals(c);
        public override int GetHashCode() => (base.GetHashCode() * 397) ^ (Name?.GetHashCode() ?? 0);
    }

    [JsonSubTypesAotConverter(nameof(MainClass.Discriminator))]
    [KnownSubType(typeof(SomeSubtype), "some")]
    public abstract class MainClass
    {
        public string Discriminator { get; set; } = "";
    }

    public class SomeSubtype : MainClass
    {
    }

    [JsonSubTypesAotConverter("Sound")]
    [KnownSubType(typeof(PDog), "Bark")]
    [KnownSubType(typeof(PCat), "Meow")]
    public interface IAnimal
    {
    }

    public class PDog : IAnimal
    {
        public string? Breed { get; set; }
    }

    public class PCat : IAnimal
    {
        public bool Declawed { get; set; }
    }

    public enum PayloadDiscriminator
    {
        GAME = 0,
        COM = 1
    }

    public enum GameDiscriminator
    {
        RUN = 0,
        WALK = 1
    }

    [JsonSubTypesAotConverter("$PayloadKind")]
    [KnownSubType(typeof(Game), PayloadDiscriminator.GAME)]
    [KnownSubType(typeof(Com), PayloadDiscriminator.COM)]
    public class Payload
    {
    }

    public class Com : Payload
    {
    }

    [JsonSubTypesAotConverter("$GameKind")]
    [KnownSubType(typeof(Run), GameDiscriminator.RUN)]
    [KnownSubType(typeof(Walk), GameDiscriminator.WALK)]
    public class Game : Payload
    {
    }

    public class Run : Game
    {
    }

    public class Walk : Game
    {
    }

    [JsonSubTypesAotConverter("Kind")]
    [KnownSubType(typeof(PDog), "Dog")]
    public class PAnimal
    {
        public string? Name { get; set; }

        [JsonIgnore]
        public string? Secret { get; set; }
    }

    [JsonSubTypesAotConverter]
    [KnownSubTypeWithProperty(typeof(PEmployee), "JobTitle")]
    [KnownSubTypeWithProperty(typeof(PEmployee), "Department")]
    [KnownSubTypeWithProperty(typeof(PArtist), "Skill")]
    public class MultiPropBase
    {
        public string? FirstName { get; set; }
    }

    public class PEmployee : MultiPropBase
    {
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
    }

    public class PArtist : MultiPropBase
    {
        public string? Skill { get; set; }
    }

    [JsonSubTypesAotConverter("Kind")]
    [KnownSubType(typeof(ParameterizedDerived), "Derived")]
    public class ParameterizedBase
    {
        public ParameterizedBase(string name)
        {
        }
    }

    public class ParameterizedDerived : ParameterizedBase
    {
        public ParameterizedDerived() : base("x")
        {
        }
    }

    [JsonSubTypesAotConverter("msgType")]
    [KnownSubType(typeof(Foo), 1)]
    public abstract class DtoBase
    {
    }

    public class Foo : DtoBase
    {
        public int MsgType { get; set; }
    }
}
