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


        [TestFixture]
        public class DiscriminatorIsANullableValueType
        {
            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass0), null)]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass1), SubType.WithAaaField)]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass2), SubType.WithZzzField)]
            public class SubTypeClassBase
            {
                public SubType? SubTypeType { get; set; }
            }

            public class SubTypeClass0 : SubTypeClassBase
            {
                public string ZeroField { get; set; }
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

                obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"ZeroField\":\"Jack\",\"SubTypeType\": null}}");
                Assert.AreEqual("Jack", (obj.SubTypeData as SubTypeClass0)?.ZeroField);
            }
        }

        [TestFixture]
        public class DiscriminatorIsANullableRef
        {

            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            [JsonSubtypes.KnownSubType(typeof(SubTypeClass1), "SubTypeClass1")]
            [JsonSubtypes.KnownSubType(typeof(NullDiscriminatorClass), null)]
            public class SubTypeClassBase
            {
                public string SubTypeType { get; set; }
            }

            public class NullDiscriminatorClass : SubTypeClassBase
            {
                public string CrazyTypeField { get; set; }
            }


            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            [Test]
            public void Deserialize()
            {
                var obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"AaaField\":\"aaa\",\"SubTypeType\": \"SubTypeClass1\"}}");
                Assert.AreEqual("aaa", (obj.SubTypeData as SubTypeClass1)?.AaaField);

                obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"CrazyTypeField\":\"Jack\",\"SubTypeType\": null}}");
                Assert.AreEqual("Jack", (obj.SubTypeData as NullDiscriminatorClass)?.CrazyTypeField);
            }
        }

    }

    public class KnownBaseType_DiscriminatorOfDifferentKindTests
    {
        [TestFixture]
        public class DiscriminatorIsAnEnum
        {
            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            public class SubTypeClassBase
            {
                public SubType SubTypeType { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), SubType.WithAaaField)]
            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), SubType.WithZzzField)]
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
            public class SubTypeClassBase
            {
                public SubType SubTypeType { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), SubType.WithAaaField)]
            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), SubType.WithZzzField)]
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
            class Child
            {
                public virtual int ChildType { get; }
            }

            [JsonSubtypes.KnownBaseType(typeof(Child), 1)]
            class Child1 : Child
            {
                public override int ChildType { get; } = 1;
            }

            [JsonSubtypes.KnownBaseType(typeof(Child), 2)]
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


        [TestFixture]
        public class DiscriminatorIsANullableValueType
        {
            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            public class SubTypeClassBase
            {
                public SubType? SubTypeType { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), null)]
            public class SubTypeClass0 : SubTypeClassBase
            {
                public string ZeroField { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), SubType.WithAaaField)]
            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), SubType.WithZzzField)]
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

                obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"ZeroField\":\"Jack\",\"SubTypeType\": null}}");
                Assert.AreEqual("Jack", (obj.SubTypeData as SubTypeClass0)?.ZeroField);
            }
        }

        [TestFixture]
        public class DiscriminatorIsANullableRef
        {

            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonConverter(typeof(JsonSubtypes), "SubTypeType")]
            public class SubTypeClassBase
            {
                public string SubTypeType { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), null)]
            public class NullDiscriminatorClass : SubTypeClassBase
            {
                public string CrazyTypeField { get; set; }
            }

            [JsonSubtypes.KnownBaseType(typeof(SubTypeClassBase), "SubTypeClass1")]
            public class SubTypeClass1 : SubTypeClassBase
            {
                public string AaaField { get; set; }
            }

            [Test]
            public void Deserialize()
            {
                var obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"AaaField\":\"aaa\",\"SubTypeType\": \"SubTypeClass1\"}}");
                Assert.AreEqual("aaa", (obj.SubTypeData as SubTypeClass1)?.AaaField);

                obj = JsonConvert.DeserializeObject<MainClass>("{\"SubTypeData\":{\"CrazyTypeField\":\"Jack\",\"SubTypeType\": null}}");
                Assert.AreEqual("Jack", (obj.SubTypeData as NullDiscriminatorClass)?.CrazyTypeField);
            }
        }

    }
}
