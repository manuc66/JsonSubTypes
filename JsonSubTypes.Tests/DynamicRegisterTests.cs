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

        public abstract class Fish : Animal
        {
            [JsonProperty("fins", Order = 2)]
            public uint FinCount { get; set; }
        }

        public class Shark : Fish
        {
            [JsonProperty("teethRows", Order = 3)]
            public uint TeethRows { get; set; }
        }

        public class HammerheadShark : Shark
        {
            [JsonProperty("hammerSize", Order = 4)]
            public float HammerSize { get; set; }
        }

        public enum AnimalType
        {
            Dog = 1,
            Cat = 2,
            Shark = 3,
            HammerheadShark = 4
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

            var json = "{\"catLives\":6,\"type\":2,\"age\":11}";

            var result = JsonConvert.DeserializeObject<Animal>(json, settings);


            Assert.Equal(typeof(Cat), result.GetType());
            Assert.Equal(11, result.Age);
            Assert.Equal(6, (result as Cat)?.Lives);
        }

        [Fact]
        public void DeserializeIncompleteTest()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"type\":2}";

            var result = JsonConvert.DeserializeObject<Animal>(json, settings);


            Assert.Equal(typeof(Cat), result.GetType());
            Assert.Equal(0, result.Age);
            Assert.Equal(7, (result as Cat)?.Lives);
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

            var json = "{\"catLives\":6,\"age\":11,\"type\":2}";

            var result = JsonConvert.SerializeObject(new Cat() { Age = 11, Lives = 6 }, settings);

            Assert.Equal(json, result);
        }

        [Fact]
        public void UnregisteredTypeSerializeTest()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4,\"hammerSize\":42.1}"; // no type property shall be added

            var result = JsonConvert.SerializeObject(new HammerheadShark
            {
                Age = 11, FinCount = 4, HammerSize = 42.1f, TeethRows = 4
            }, settings);

            Assert.Equal(json, result);
        }


        [Fact]
        public void UnregisteredTypeSerializeTest2()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                // Shark is not registered
                .RegisterSubtype(typeof(HammerheadShark), AnimalType.HammerheadShark)
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4}"; // no type property shall be added

            var result = JsonConvert.SerializeObject(new Shark()
            {
                Age = 11,
                FinCount = 4,
                TeethRows = 4
            }, settings);

            Assert.Equal(json, result);
        }

        [Fact]
        public void UnregisteredTypeDeserializeTest()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                // HammerheadShark is not registered
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4,\"hammerSize\":42.1,\"type\":4}";

            var exception =  Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<Animal>(json, settings));

            Assert.Contains("Type is an interface or abstract class and cannot be instantiated.", exception.Message);
        }

        [Fact]
        public void NestedTypeDeserializeTest()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { };
            var settings = JsonConvert.DefaultSettings();
            settings.Converters.Add(JsonSubtypesWithoutExplicitTypePropertyConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                // Shark is not registered
                .RegisterSubtype(typeof(HammerheadShark), AnimalType.HammerheadShark)
                .Build());

            var json = "{\"age\":11,\"fins\":3,\"teethRows\":4,\"hammerSize\":42.1,\"type\":4}";

            var result = JsonConvert.DeserializeObject<Animal>(json, settings);
            

            Assert.Equal(typeof(HammerheadShark), result.GetType());
            Assert.Equal(11, result.Age);
            Assert.Equal(3u, (result as Fish)?.FinCount);
            Assert.Equal(4u, (result as Shark)?.TeethRows);
            Assert.Equal(42.1f, (result as HammerheadShark)?.HammerSize);
        }
    }
}
