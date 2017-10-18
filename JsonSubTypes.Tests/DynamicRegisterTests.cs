using System;
using Newtonsoft.Json;
using Xunit;

namespace JsonSubTypes.Tests
{
    public class DynamicRegisterTests
    {
        public abstract class Animal
        {
            [JsonProperty("age", Order = 1)]
            public int Age { get; set; }
        }

        public class Dog : Animal
        {
            public bool CanBark { get; set; } = true;
        }

        public class Cat : Animal
        {
            [JsonProperty("catLives", Order = 0)]
            public int Lives { get; set; } = 7;
        }

        public enum AnimalType
        {
            Dog = 1,
            Cat = 2
        }

        [Fact]
        public void SerializeTest()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"type\":2,\"age\":11}";

            var result = JsonConvert.DeserializeObject<Animal>(json, settings);


            Assert.Equal(typeof(Cat), result.GetType());
            Assert.Equal(11, result.Age);
            Assert.Equal(6, (result as Cat)?.Lives);
        }

        [Fact]
        public void DeserializeTest()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"age\":11,\"type\":2}";

            var result = JsonConvert.SerializeObject(new Cat() { Age = 11, Lives = 6 }, settings);

            Assert.Equal(json, result);
        }

        public interface IExpression
        {

            string Type { get; }
        }

        public class BinaryExpression : IExpression
        {
            public IExpression SubExpressionA { get; set; }
            public IExpression SubExpressionB { get; set; }
            public string Type { get { return "Binary"; } }
        }

        public class ConstantExpression : IExpression
        {
            public string Value { get; set; }
            public string Type { get { return "Constant"; } }
        }

        [Fact]
        public void TestIfNestedObjectIsDeserialized()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(BinaryExpression), "Binary")
                .Build());

            var binary = JsonConvert.DeserializeObject<IExpression>("{\"Type\":\"Binary\"," +
                                                                    "\"SubExpressionA\":{\"Type\":\"Constant\",\"Value\":\"A\"}," +
                                                                    "\"SubExpressionB\":{\"Type\":\"Constant\",\"Value\":\"B\"}" +
                                                                    "}", settings);
            Assert.Equal(typeof(ConstantExpression), (binary as BinaryExpression)?.SubExpressionA.GetType());
        }

        [Fact]
        public void TestIfNestedObjectIsSerialized()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(BinaryExpression), "Binary")
                .Build());

            var json = JsonConvert.SerializeObject(new BinaryExpression()
            {
                SubExpressionA = new ConstantExpression() { Value = "A" },
                SubExpressionB = new ConstantExpression() { Value = "B" }
            }, settings);

            Assert.Equal("{" +
                "\"SubExpressionA\":{\"Value\":\"A\",\"Type\":\"Constant\"}," +
                "\"SubExpressionB\":{\"Value\":\"B\",\"Type\":\"Constant\"}" +
                ",\"Type\":\"Binary\"}", json);
        }
    }
}
