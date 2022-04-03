using System.Text.Json;
using JsonSubTypes.Text.Json;
using NewApi;
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

            [JsonSubTypeConverter(typeof(JsonSubtypes<SubTypeClassBase>), "SubTypeType")]
            [KnownSubType(typeof(SubTypeClass1), SubType.WithAaaField)]
            [KnownSubType(typeof(SubTypeClass2), SubType.WithZzzField)]
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
                var obj = JsonSerializer.Deserialize<MainClass>("{\"SubTypeData\":{\"ZzzField\":\"zzz\",\"SubTypeType\":1}}");
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

            [JsonSubTypeConverter(typeof(JsonSubtypes<Child>), "ChildType")]
            [KnownSubType(typeof(Child1), 1)]
            [KnownSubType(typeof(Child2), 2)]
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
                var root1 = JsonSerializer.Deserialize<Parent>("{\"child\":{\"ChildType\":1}}");
                var root2 = JsonSerializer.Deserialize<Parent>("{\"child\":{\"ChildType\":2}}");
                
                Assert.NotNull(root1.child as Child1);
                Assert.NotNull(root2.child as Child2);
            }
            
            [Test]
            [Ignore("Not supported")]
            public void DiscriminatorValueCanBeANumberFallBackDefault()
            {
                //var root3 = JsonSerializer.Deserialize<Parent>("{\"child\":{\"ChildType\":8}}");
                // var root4 = JsonSerializer.Deserialize<Parent>("{\"child\":{\"ChildType\":null}}");
//                var root5 = JsonSerializer.Deserialize<Parent>("{\"child\":{}}");

                // Assert.AreEqual(typeof(Child), root3.child.GetType());
                // Assert.AreEqual(typeof(Child), root4.child.GetType());
                //    Assert.AreEqual(typeof(Child), root5.child.GetType());
            }
        }


        [TestFixture]
        public class DiscriminatorIsANullableValueType
        {
            public class MainClass
            {
                public SubTypeClassBase SubTypeData { get; set; }
            }

            [JsonSubTypeConverter(typeof(JsonSubtypes<SubTypeClassBase>), "SubTypeType")]
            [KnownSubType(typeof(SubTypeClass0), null)]
            [KnownSubType(typeof(SubTypeClass1), SubType.WithAaaField)]
            [KnownSubType(typeof(SubTypeClass2), SubType.WithZzzField)]
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
                var obj = JsonSerializer.Deserialize<MainClass>("{\"SubTypeData\":{\"ZzzField\":\"zzz\",\"SubTypeType\":1}}");
                Assert.AreEqual("zzz", (obj.SubTypeData as SubTypeClass2)?.ZzzField);

                obj = JsonSerializer.Deserialize<MainClass>("{\"SubTypeData\":{\"ZeroField\":\"Jack\",\"SubTypeType\": null}}");
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

            [JsonSubTypeConverter(typeof(JsonSubtypes<SubTypeClassBase>), "SubTypeType")]
            [KnownSubType(typeof(SubTypeClass1), "SubTypeClass1")]
            [KnownSubType(typeof(NullDiscriminatorClass), null)]
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
                var obj = JsonSerializer.Deserialize<MainClass>("{\"SubTypeData\":{\"AaaField\":\"aaa\",\"SubTypeType\": \"SubTypeClass1\"}}");
                Assert.AreEqual("aaa", (obj.SubTypeData as SubTypeClass1)?.AaaField);

                obj = JsonSerializer.Deserialize<MainClass>("{\"SubTypeData\":{\"CrazyTypeField\":\"Jack\",\"SubTypeType\": null}}");
                Assert.AreEqual("Jack", (obj.SubTypeData as NullDiscriminatorClass)?.CrazyTypeField);
            }
        }

    }
}
