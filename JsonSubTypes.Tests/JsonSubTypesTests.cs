using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace JsonSubTypes.Tests
{
    class Root
    {
        public Base Content { get; set; }

        protected bool Equals(Root other)
        {
            return Equals(Content, other.Content);
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
            return Content != null ? Content.GetHashCode() : 0;
        }
    }

    [JsonConverter(typeof(JsonSubtypes), "@type")]
    [JsonKnownSubType(typeof(SubB), "SubB")]
    [JsonKnownSubType(typeof(SubC), "SubC")]
    class Base
    {
        [JsonProperty("@type")]
        public virtual string Type { get; }

        protected bool Equals(Base other)
        {
            return string.Equals(Type, other.Type);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return GetType().IsInstanceOfType(obj) && Equals((Base)obj);
        }

        public override int GetHashCode()
        {
            return Type != null ? Type.GetHashCode() : 0;
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
            return GetType().IsInstanceOfType(obj) && Equals((SubB)obj);
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
            return GetType().IsInstanceOfType(obj) && Equals((SubC)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }
    }
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SerializeTest()
        {

            var root = new Root
            {
                Content = new SubB { Index = 1 }
            };

            string str = JsonConvert.SerializeObject(root);

            Assert.AreEqual("{\"Content\":{\"@type\":\"SubB\",\"Index\":1}}", str);
        }

        [TestMethod]
        public void BadDeserializeTest()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}");

            Assert.AreEqual(expected, root);
        }

        [TestMethod]
        public void DeserializeTest()
        {
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":\"SubB\"}}");

            Assert.AreEqual(expected, root);
        }
    }
}
