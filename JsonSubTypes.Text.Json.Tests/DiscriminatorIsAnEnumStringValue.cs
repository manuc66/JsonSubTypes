using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DiscriminatorIsAnEnumStringValue
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
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            var obj = JsonSerializer.Deserialize<MainClass>(
                "{\"SubTypeData\":{\"ZzzField\":\"zzz\",\"SubTypeType\":\"WithZzzField\"}}", options);

            Assert.AreEqual("zzz", (obj.SubTypeData as SubTypeClass2)?.ZzzField);
        }
    }
}
