using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DemoKnownSubTypeWithMultipleProperties
    {
        [JsonSubTypeConverter(typeof(JsonSubtypes<Person>))]
        [KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
        [KnownSubTypeWithProperty(typeof(Employee), "Department")]
        [KnownSubTypeWithProperty(typeof(Artist), "Skill")]
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
            string json = "[{\"Department\":\"Department1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                          "{\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                          "{\"Skill\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";

            var persons = JsonSerializer.Deserialize<ICollection<Person>>(json);
            Assert.AreEqual("Painter", (persons.Last() as Artist)?.Skill);
        }

        [Test]
        public void DemoDifferentCase()
        {
            string json = "[{\"Department\":\"Department1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                          "{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                          "{\"skill\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var persons = JsonSerializer.Deserialize<ICollection<Person>>(json, options);
            Assert.AreEqual("Painter", (persons.Last() as Artist)?.Skill);
        }

        [Test]
        public void FallBackToPArentWhenNotFound()
        {
            string json = "[{\"Skl.\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";

            var persons = JsonSerializer.Deserialize<ICollection<Person>>(json);
            Assert.AreEqual(typeof(Person), persons.First().GetType());
        }

        [Test]
        public void ThrowIfManyMatches()
        {
            string json = "{\"Name\":\"Foo\",\"Skill\":\"A\",\"JobTitle\":\"B\"}";

            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Person>(json));

            Assert.AreEqual(
                "Ambiguous type resolution, expected only one type but got: JsonSubTypes.Tests.DemoKnownSubTypeWithMultipleProperties+Employee, JsonSubTypes.Tests.DemoKnownSubTypeWithMultipleProperties+Artist",
                exception.Message);
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<ClassA>))]
        [KnownSubTypeWithProperty(typeof(ClassC), nameof(ClassC.Other), StopLookupOnMatch = true)]
        [KnownSubTypeWithProperty(typeof(ClassB), nameof(ClassB.Optional))]
        [FallBackSubType(typeof(ClassB))]
        public class ClassA
        {
            public string CommonProp { get; set; }
        }

        public class ClassB : ClassA
        {
            public bool? Optional { get; set; }
        }

        public class ClassC : ClassB
        {
            public string Other { get; set; }
        }

        [Test]
        public void StopLookupOnMatch()
        {
            string json = "{\"CommonProp\": null, \"Optional\": null, \"Other\": null}";

            ClassA deserializeObject = JsonSerializer.Deserialize<ClassA>(json);

            Assert.IsInstanceOf<ClassC>(deserializeObject);
        }
    }
}
