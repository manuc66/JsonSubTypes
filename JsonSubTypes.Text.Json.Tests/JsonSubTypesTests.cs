using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewApi;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    class Root
    {
        public Base Content { get; set; }
        public List<Base> ContentList { get; set; }

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

        public override bool Equals(object obj)
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

    [JsonSubTypeConverter(typeof(JsonSubtypes<Base>), "@type")]
    [KnownSubType(typeof(SubB), "SubB")]
    [KnownSubType(typeof(SubC), "SubC")]
    class Base
    {
        [JsonPropertyName("@type")]
        public virtual string Type { get; }

        [JsonPropertyName("4-you")]
        public int _4You { get; set; }

        protected bool Equals(Base other)
        {
            return string.Equals(Type, other.Type) && _4You == other._4You;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Base && Equals((Base)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Type != null ? Type.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ _4You;
                return hashCode;
            }
        }
    }

    class SubB : Base
    {
        public override string Type { get; } = "SubB";
        public int Index { get; set; }

        protected bool Equals(SubB other)
        {
            return base.Equals(other) && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SubB && Equals((SubB)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ Index;
            }
        }
    }

    class SubC : Base
    {
        public override string Type { get; } = "SubC";

        public string Name { get; set; }

        protected bool Equals(SubC other)
        {
            return base.Equals(other) && string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is SubC && Equals((SubC)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
    [TestFixture]
    public class JsonSubTypesTests
    {
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

            string str = JsonSerializer.Serialize(root);

            Assert.AreEqual("{\"Content\":{\"@type\":\"SubB\",\"Index\":1,\"4-you\":2},\"ContentList\":null}", str);
        }

        [Test]
        public void DeserializeSubType()
        {
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":\"SubB\"}}");

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeSubTypeWithComments()
        {
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var root = JsonSerializer.Deserialize<Root>(
                "{\"Content\":/* foo bar */{\"Index\":1,\"@type\":\"SubB\"}}", new JsonSerializerOptions() {ReadCommentHandling = JsonCommentHandling.Skip});

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeNull()
        {
            var expected = new Root
            {
                Content = null
            };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":null}");

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void DeserializeBadDocument()
        {
            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Root>("{\"Content\":8}"));

            Assert.AreEqual("Unrecognized token: Number", exception.Message);
        }

        [Test]
        public void WhenDiscriminatorValueIsNullDeserializeToBaseType()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}");

            Assert.AreEqual(expected, root);
        }

        [Test]
        public void WhenDiscriminatorValueIsUnknownDeserializeToBaseType()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonSerializer.Deserialize<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5}}");

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
                "{\"Content\":{\"Index\":1,\"@type\":8.5},\"ContentList\":[{\"Index\":1,\"@type\":\"SubB\"},{\"Name\":\"foo\",\"@type\":\"SubC\"}]}");

            Assert.AreEqual(expected, root);
        }
    }
}
