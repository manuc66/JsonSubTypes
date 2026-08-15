using System;
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

        [TestFixture]
        public class DiscriminatorWriteWithCustomConverters
        {
            // Covers the discriminator write fast path: string/int discriminators are written as a
            // plain JValue only when no converter on the serializer handles that type. When a custom
            // converter applies, the serializer-aware JToken.FromObject path must be used instead,
            // otherwise the converter would be silently bypassed.

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

            private class DoublingIntConverter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    writer.WriteValue(((int)value) * 2);
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                {
                    return (int)(long)reader.Value;
                }

                public override bool CanConvert(Type objectType)
                {
                    return objectType == typeof(int);
                }
            }

            private class UppercasingStringConverter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    writer.WriteValue(((string)value).ToUpperInvariant());
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                {
                    return (string)reader.Value;
                }

                public override bool CanConvert(Type objectType)
                {
                    return objectType == typeof(string);
                }
            }

            [Test]
            public void StringDiscriminatorWithoutConverterUsesFastPath()
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(JsonSubtypesConverterBuilder
                    .Of(typeof(Animal), "type")
                    .SerializeDiscriminatorProperty()
                    .RegisterSubtype(typeof(Cat), "cat")
                    .RegisterSubtype(typeof(Dog), "dog")
                    .Build());

                var json = JsonConvert.SerializeObject(new Cat { Age = 3, Lives = 9 }, settings);

                StringAssert.Contains("\"type\":\"cat\"", json);
                StringAssert.Contains("\"Age\":3", json);
                StringAssert.Contains("\"Lives\":9", json);
            }

            [Test]
            public void IntDiscriminatorWithoutConverterUsesFastPath()
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(JsonSubtypesConverterBuilder
                    .Of(typeof(Animal), "type")
                    .SerializeDiscriminatorProperty()
                    .RegisterSubtype(typeof(Cat), 1)
                    .RegisterSubtype(typeof(Dog), 2)
                    .Build());

                var json = JsonConvert.SerializeObject(new Cat { Age = 3, Lives = 9 }, settings);

                StringAssert.Contains("\"type\":1", json);
                StringAssert.Contains("\"Age\":3", json);
                StringAssert.Contains("\"Lives\":9", json);
            }

            [Test]
            public void StringDiscriminatorWithConverterKeepsSerializerAwarePath()
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new UppercasingStringConverter());
                settings.Converters.Add(JsonSubtypesConverterBuilder
                    .Of(typeof(Animal), "type")
                    .SerializeDiscriminatorProperty()
                    .RegisterSubtype(typeof(Cat), "cat")
                    .RegisterSubtype(typeof(Dog), "dog")
                    .Build());

                var json = JsonConvert.SerializeObject(new Cat { Age = 3, Lives = 9 }, settings);

                StringAssert.Contains("\"type\":\"CAT\"", json);
            }

            [Test]
            public void IntDiscriminatorWithConverterKeepsSerializerAwarePath()
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new DoublingIntConverter());
                settings.Converters.Add(JsonSubtypesConverterBuilder
                    .Of(typeof(Animal), "type")
                    .SerializeDiscriminatorProperty()
                    .RegisterSubtype(typeof(Cat), 1)
                    .RegisterSubtype(typeof(Dog), 2)
                    .Build());

                var json = JsonConvert.SerializeObject(new Cat { Age = 3, Lives = 9 }, settings);

                StringAssert.Contains("\"type\":2", json);
            }
        }

    }
}
