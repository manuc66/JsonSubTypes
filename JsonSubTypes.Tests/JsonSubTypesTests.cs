﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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
                var hashCode = (Content != null ? Content.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ContentList != null ? ContentList.Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode()) : 0);
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
            return GetType().IsInstanceOfType(obj) && Equals((Base)obj);
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
                Content = new SubB
                {
                    Index = 1,
                    _4You = 2
                }
            };

            string str = JsonConvert.SerializeObject(root);

            Assert.AreEqual("{\"Content\":{\"@type\":\"SubB\",\"Index\":1,\"4-you\":2},\"ContentList\":null}", str);
        }

        [TestMethod]
        public void DeserializeSubType()
        {
            var expected = new Root
            {
                Content = new SubB { Index = 1 }
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":\"SubB\"}}");

            Assert.AreEqual(expected, root);
        }

        [TestMethod]
        public void DeserializeNull()
        {
            var expected = new Root
            {
                Content = null
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":null}");

            Assert.AreEqual(expected, root);
        }


        [TestMethod]
        public void WhenDiscriminatorValueIsNullDeserializeToBaseType()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":null}}");

            Assert.AreEqual(expected, root);
        }

        [TestMethod]
        public void WhenDiscriminatorValueIsUnknownDeserializeToBaseType()
        {
            var expected = new Root
            {
                Content = new Base()
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5}}");

            Assert.AreEqual(expected, root);
        }

        [TestMethod]
        public void WorkWithSubList()
        {
            var expected = new Root
            {
                Content = new Base(),
                ContentList = new List<Base> { new SubB { Index = 1 }, new SubC { Name = "foo" } }
            };

            var root = JsonConvert.DeserializeObject<Root>("{\"Content\":{\"Index\":1,\"@type\":8.5},\"ContentList\":[{\"Index\":1,\"@type\":\"SubB\"},{\"Name\":\"foo\",\"@type\":\"SubC\"}]}");

            Assert.AreEqual(expected, root);
        }

        class Parent
        {
            public Child child { get; set; }
        }

        [JsonConverter(typeof(JsonSubtypes), "ChildType")]
        [JsonSubtypes.KnownSubType(typeof(Child1), 1)]
        [JsonSubtypes.KnownSubType(typeof(Child2), 2)]
        class Child
        {
            public virtual int ChildType { get; }
        }

        class Child1 : Child
        {
            public override int ChildType { get; } = 1;
        }
        class Child2 : Child
        {
            public override int ChildType { get; } = 2;
        }

        [TestMethod]
        public void DiscriminatorValueCanBeANumber()
        {
            var root1 = JsonConvert.DeserializeObject<Parent>("{\"Child\":{\"ChildType\":1}}");
            var root2 = JsonConvert.DeserializeObject<Parent>("{\"Child\":{\"ChildType\":2}}");
            var root3 = JsonConvert.DeserializeObject<Parent>("{\"Child\":{\"ChildType\":8}}");
            var root4 = JsonConvert.DeserializeObject<Parent>("{\"Child\":{\"ChildType\":null}}");
            var root5 = JsonConvert.DeserializeObject<Parent>("{\"Child\":{}}");

            Assert.IsNotNull(root1.child as Child1);
            Assert.IsNotNull(root2.child as Child2);
            Assert.AreEqual(typeof(Child), root3.child.GetType());
            Assert.AreEqual(typeof(Child), root4.child.GetType());
            Assert.AreEqual(typeof(Child), root5.child.GetType());
        }
    }
}
