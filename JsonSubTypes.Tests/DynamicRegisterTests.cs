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
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
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
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"age\":11,\"type\":2}";

            var result = JsonConvert.SerializeObject(new Cat() { Age = 11, Lives = 6 }, settings);

            Assert.Equal(json, result);
        }
    }
}
