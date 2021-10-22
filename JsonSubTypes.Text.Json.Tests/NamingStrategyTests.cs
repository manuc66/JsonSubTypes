using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
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
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy(),
                },
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    },
                    JsonSubtypesConverterBuilder
                        .Of(typeof(IMyType), "enum_value")
                        .RegisterSubtype(typeof(MyTypeOne), EnumType.EnumMemberOne)
                        .RegisterSubtype(typeof(MyTypeTwo), EnumType.EnumMemberTwo)
                        .Build()
                }
            };

            var json = "{\"enum_value\":\"enum_member_one\"}";
            var result = JsonConvert.DeserializeObject<IMyType>(json, serializerSettings);

            var serializeObject = JsonConvert.SerializeObject(result, serializerSettings);

            Assert.AreEqual(json, serializeObject);
        }
    }
}
