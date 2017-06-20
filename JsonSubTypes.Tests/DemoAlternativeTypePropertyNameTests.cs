using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

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

        [TestClass]
        public class DemoAlternativeTypePropertyWithFullNameTests
        {

            [TestMethod]
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

        [TestClass]
        public class DemoAlternativeTypePropertyNameTests
        {
            [TestMethod]
            public void Demo()
            {
                var annimal =
                    JsonConvert.DeserializeObject<Annimal>(
                        "{\"ClassName\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            }
        }
    }
}
