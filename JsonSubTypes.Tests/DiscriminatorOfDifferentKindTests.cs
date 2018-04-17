using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class DiscriminatorOfDifferentKindTests
    {
        [TestFixture]
        public class DiscriminatorIsAnEnum
        {
            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass1), SubType.WithAaaField)]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass2), SubType.WithZzzField)]
            public class SubTypeClassBase
            {
                public SubType SubTypeType { get; set; }
            }

            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            public class SubTypeClass2 : SubTypeClassBase
            {
                public string ZzzField { get; set; }
            }

            public enum SubType
            {
                WithAaaField,
                WithZzzField
            }

            [Test]
            public void Deserialize()
            {
                var obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"ZzzField\":\"zzz\",\"SubTypeType\":1}}");
                Assert.AreEqual("zzz", (obj.SubTypeData as SubTypeClass2)?.ZzzField);
            }
        }
        [TestFixture]
        public class DiscriminatorIsAnEnumStringValue
        {
            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass1), SubType.WithAaaField)]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass2), SubType.WithZzzField)]
            public class SubTypeClassBase
            {
                public SubType SubTypeType { get; set; }
            }

            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            public class SubTypeClass2 : SubTypeClassBase
            {
                public string ZzzField { get; set; }
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public enum SubType
            {
                WithAaaField,
                [EnumMember(Value = "zzzField")]
                WithZzzField
            }

            [Test]
            public void Deserialize()
            {
                var obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"ZzzField\":\"zzz\",\"SubTypeType\":\"zzzField\"}}");
                Assert.AreEqual("zzz", (obj.SubTypeData as SubTypeClass2)?.ZzzField);
            }
        }

        [TestFixture]
        public class DiscriminatorIsAnInt
        {
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

            [Test]
            public void DiscriminatorValueCanBeANumber()
            {
                var root1 = JsonConvert.DeserializeObject<Parent>("{\"child\":{\"ChildType\":1}}");
                var root2 = JsonConvert.DeserializeObject<Parent>("{\"child\":{\"ChildType\":2}}");
                var root3 = JsonConvert.DeserializeObject<Parent>("{\"child\":{\"ChildType\":8}}");
                var root4 = JsonConvert.DeserializeObject<Parent>("{\"child\":{\"ChildType\":null}}");
                var root5 = JsonConvert.DeserializeObject<Parent>("{\"child\":{}}");

                Assert.NotNull(root1.child as Child1);
                Assert.NotNull(root2.child as Child2);
                Assert.AreEqual(typeof(Child), root3.child.GetType());
                Assert.AreEqual(typeof(Child), root4.child.GetType());
                Assert.AreEqual(typeof(Child), root5.child.GetType());
            }
        }
    }
}
