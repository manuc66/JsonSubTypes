using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Xunit;

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
            return Equals((Root) obj);
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

    [JsonConverter(typeof(JsonSubtypes), "@type")]
    [JsonSubtypes.KnownSubType(typeof(SubB), "SubB")]
    [JsonSubtypes.KnownSubType(typeof(SubC), "SubC")]
    class Base
    {
        [JsonProperty("@type")]
        public virtual string Type { get; }

        [JsonProperty("4-you")]
        public int _4You { get; set; }

        protected bool Equals(Base other)
        {
            return string.Equals(Type, other.Type) && _4You == other._4You;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return GetType().IsInstanceOfType(obj) && Equals((Base) obj);
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
            return GetType().IsInstanceOfType(obj) && Equals((SubB) obj);
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
            return GetType().IsInstanceOfType(obj) && Equals((SubC) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }

    public class JsonSubTypesTests
    {
        [Fact]
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

            string str = JsonConvert.SerializeObject(root);

            Assert.Equal("{\"Content\":{\"@type\":\"SubB\",\"Index\":1,\"4-you\":2},\"ContentList\":null}", str);
        }

        [Fact]
        public void DeserializeSubType()
        {
            var expected = new Root
            {
                Content = new SubB {Index = 1}
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":\"SubB\"}}");

            Assert.Equal(expected, root);
        }

        [Fact]
        public void DeserializeSubTypeWithComments()
        {
            var expected = new Root
            {
                Content = new SubB {Index = 1}
            };

            var root = JsonConvert.DeserializeObject<Root>(
                "{\"Content\":/* foo bar */{\"Index\":1,\"@type\":\"SubB\"}}");

            Assert.Equal(expected, root);
        }

        [Fact]
        public void DeserializeNull()
        {
            var expected = new Root
            {
                Content = null
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":null}");

            Assert.Equal(expected, root);
        }

        [Fact]
        public void DeserializeBadDocument()
        {
            Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<Root>("{\"Content\":8}"));
        }

        [Fact]
        public void WhenDiscriminatorValueIsNullDeserializeToBaseType()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}");

            Assert.Equal(expected, root);
        }

        [Fact]
        public void WhenDiscriminatorValueIsUnknownDeserializeToBaseType()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5}}");

            Assert.Equal(expected, root);
        }

        [Fact]
        public void WorkWithSubList()
        {
            var expected = new Root
            {
                Content = new Base(),
                ContentList = new List<Base> {new SubB {Index = 1}, new SubC {Name = "foo"}}
            };

            var root = JsonConvert.DeserializeObject<Root>(
                "{\"Content\":{\"Index\":1,\"@type\":8.5},\"ContentList\":[{\"Index\":1,\"@type\":\"SubB\"},{\"Name\":\"foo\",\"@type\":\"SubC\"}]}");

            Assert.Equal(expected, root);
        }
    }
}