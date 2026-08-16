using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
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
            public void DiscriminatorValueCanBeANumberFallBackDefault()
            {
                var root3 = JsonSerializer.Deserialize<Parent>("{\"child\":{\"ChildType\":8}}");
                var root4 = JsonSerializer.Deserialize<Parent>("{\"child\":{\"ChildType\":null}}");
                var root5 = JsonSerializer.Deserialize<Parent>("{\"child\":{}}");

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

        [TestFixture]
        public class DiscriminatorWriteWithCustomConverters
        {
            // The System.Text.Json write path always serializes the discriminator through
            // JsonSerializer.Serialize, so converters registered on the options apply. These tests
            // pin that behavior: a custom converter on the discriminator type must be honored.

            public class Animal
            {
                public int Age { get; set; }
            }

            public class Cat : Animal
            {
                public int Lives { get; set; }
            }

            public class Dog : Animal
            {
                public bool CanHunt { get; set; }
            }

            private class DoublingIntConverter : JsonConverter<int>
            {
                public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    return reader.GetInt32();
                }

                public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
                {
                    writer.WriteNumberValue(value * 2);
                }
            }

            private class UppercasingStringConverter : JsonConverter<string>
            {
                public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    return reader.GetString();
                }

                public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
                {
                    writer.WriteStringValue(value.ToUpperInvariant());
                }
            }

            [Test]
            public void StringDiscriminatorSerializes()
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Animal>("type")
                    .RegisterSubtype<Cat>("cat")
                    .RegisterSubtype<Dog>("dog")
                    .SerializeDiscriminatorProperty()
                    .Build());

                var json = JsonSerializer.Serialize<Animal>(new Cat { Age = 3, Lives = 9 }, options);

                StringAssert.Contains("\"type\":\"cat\"", json);
                StringAssert.Contains("\"Age\":3", json);
                StringAssert.Contains("\"Lives\":9", json);
            }

            [Test]
            public void IntDiscriminatorSerializes()
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Animal>("type")
                    .RegisterSubtype<Cat>(1)
                    .RegisterSubtype<Dog>(2)
                    .SerializeDiscriminatorProperty()
                    .Build());

                var json = JsonSerializer.Serialize<Animal>(new Cat { Age = 3, Lives = 9 }, options);

                StringAssert.Contains("\"type\":1", json);
                StringAssert.Contains("\"Age\":3", json);
                StringAssert.Contains("\"Lives\":9", json);
            }

            [Test]
            public void StringDiscriminatorHonorsStringConverter()
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new UppercasingStringConverter());
                options.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Animal>("type")
                    .RegisterSubtype<Cat>("cat")
                    .RegisterSubtype<Dog>("dog")
                    .SerializeDiscriminatorProperty()
                    .Build());

                var json = JsonSerializer.Serialize<Animal>(new Cat { Age = 3, Lives = 9 }, options);

                StringAssert.Contains("\"type\":\"CAT\"", json);
            }

            [Test]
            public void IntDiscriminatorHonorsIntConverter()
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new DoublingIntConverter());
                options.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Animal>("type")
                    .RegisterSubtype<Cat>(1)
                    .RegisterSubtype<Dog>(2)
                    .SerializeDiscriminatorProperty()
                    .Build());

                var json = JsonSerializer.Serialize<Animal>(new Cat { Age = 3, Lives = 9 }, options);

                StringAssert.Contains("\"type\":2", json);
            }
        }

    }
}
