using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    namespace FullyQualifiedName
    {
        [JsonConverter(typeof(JsonSubtypes), "ClassName")]
        public class Annimal
        {
            public virtual string ClassName { get; }
            public string Color { get; set; }
        }

        public class Dog : Annimal
        {
            public override string ClassName { get; } = typeof(Dog).FullName;
            public string Breed { get; set; }
        }

        public class Cat : Annimal
        {
            public override string ClassName { get; } = typeof(Cat).FullName;
            public bool Declawed { get; set; }
        }

        public class DemoAlternativeTypePropertyWithFullNameTests
        {
            [Test]
            public void Demo()
            {
                var annimal =
                    JsonConvert.DeserializeObject<Annimal>(
                        "{\"ClassName\":\"JsonSubTypes.Tests.FullyQualifiedName.Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            }
        }
    }

    namespace SimplyQualifiedName
    {
        [JsonConverter(typeof(JsonSubtypes), "ClassName")]
        public class Annimal
        {
            public virtual string ClassName { get; }
            public string Color { get; set; }
        }

        public class Dog : Annimal
        {
            public override string ClassName { get; } = typeof(Dog).Name;
            public string Breed { get; set; }
        }

        public class Cat : Annimal
        {
            public override string ClassName { get; } = typeof(Cat).Name;
            public bool Declawed { get; set; }
        }


        public class DemoAlternativeTypePropertyNameTests
        {
            [Test]
            public void Demo()
            {
                var annimal =
                    JsonConvert.DeserializeObject<Annimal>(
                        "{\"ClassName\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            }

            [Test]
            public void DemoCaseInsensitive()
            {
                var annimal =
                    JsonConvert.DeserializeObject<Annimal>(
                        "{\"ClassName\":\"dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            }
        }
    }

    namespace SimplyQualifiedNameNestedType
    {
        public class DemoAlternativeTypePropertyNameTests
        {
            [JsonConverter(typeof(JsonSubtypes), "Kind")]
            public interface IAnnimal
            {
                string Kind { get; }
            }

            public class Dog : IAnnimal
            {
                public string Kind { get; } = "Dog";
                public string Breed { get; set; }
            }

            [Test]
            public void Demo()
            {
                var annimal =
                    JsonConvert.DeserializeObject<IAnnimal>(
                        "{\"Kind\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            }

            [Test]
            public void DemoCaseInsensitive()
            {
                var annimal =
                    JsonConvert.DeserializeObject<IAnnimal>(
                        "{\"Kind\":\"dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            }

            [Test]
            public void WhenNoMappingPossible()
            {
                Assert.Throws<JsonSerializationException>(() =>
                    JsonConvert.DeserializeObject<IAnnimal>("{\"Kind\":8,\"Breed\":\"Jack Russell Terrier\"}")
                );
            }
        }
    }
}