using System.Runtime.Serialization;
using NewApi;
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

        [JsonSubTypeConverter(typeof(StringEnumConverter))]
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
}
