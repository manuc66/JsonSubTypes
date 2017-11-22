using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DemoKnownSubTypeWithProperty
    {
        [JsonConverter(typeof(JsonSubtypes))]
        [JsonSubtypes.KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
        [JsonSubtypes.KnownSubTypeWithProperty(typeof(Artist), "Skill")]
        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class Employee : Person
        {
            public string Department { get; set; }
            public string JobTitle { get; set; }
        }

        public class Artist : Person
        {
            public string Skill { get; set; }
        }

        [Test]
        public void Demo()
        {
            string json = "[{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                          "{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                          "{\"Skill\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";


            var persons = JsonConvert.DeserializeObject<ICollection<Person>>(json);
            Assert.AreEqual("Painter", (persons.Last() as Artist)?.Skill);
        }

        [Test]
        public void FallBackToPArentWhenNotFound()
        {
            string json = "[{\"Skl.\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";

            var persons = JsonConvert.DeserializeObject<ICollection<Person>>(json);
            Assert.AreEqual(typeof(Person), persons.First().GetType());
        }
    }
}