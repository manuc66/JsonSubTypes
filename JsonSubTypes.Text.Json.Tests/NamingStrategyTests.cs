using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class NamingStrategyTests
    {
        public enum EnumType
        {
            EnumMemberOne,
            EnumMemberTwo
        }

        public interface IMyType
        {
            EnumType EnumValue { get; }
        }

        public class MyTypeOne : IMyType
        {
            public EnumType EnumValue => EnumType.EnumMemberOne;
        }

        public class MyTypeTwo : IMyType
        {
            public EnumType EnumValue => EnumType.EnumMemberTwo;
        }

        [Test]
        public void EnumDiscriminatorPropertySupportNamingStrategy()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IMyType), "enum_value")
                .RegisterSubtype(typeof(MyTypeOne), EnumType.EnumMemberOne)
                .RegisterSubtype(typeof(MyTypeTwo), EnumType.EnumMemberTwo)
                .Build());

            var json = "{\"enum_value\":\"enum_member_one\"}";
            var result = JsonSerializer.Deserialize<IMyType>(json, options);

            var serializeObject = JsonSerializer.Serialize(result, options);

            Assert.AreEqual(json, serializeObject);
        }
    }
}
