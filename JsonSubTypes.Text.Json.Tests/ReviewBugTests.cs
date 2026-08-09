using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Tests.Plugin;
using JsonSubTypes.Tests.Shared;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class ReviewBugTests
    {
        [JsonSubTypeConverter(typeof(JsonSubtypes<Animal>), "Kind")]
        [KnownSubType(typeof(Dog), "Dog")]
        public class Animal
        {
            public string Kind { get; set; }

            [JsonIgnore]
            public string Secret { get; set; }
        }

        public class Dog : Animal
        {
            public bool CanBark { get; set; }
        }

        [Test]
        public void FallbackWriteRespectsJsonIgnore()
        {
            var json = JsonSerializer.Serialize<Animal>(new Animal { Secret = "hidden" });

            Assert.That(json, Does.Not.Contain("Secret"));
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<Container>), "Kind")]
        [KnownSubType(typeof(Dog), "Dog")]
        public class Container
        {
            public string Kind { get; set; }
            public Animal Pet { get; set; }
        }

        [Test]
        public void FallbackReadWithNestedPolymorphicProperty()
        {
            var json = "{\"Kind\":\"Unknown\",\"Pet\":{\"Kind\":\"Dog\",\"CanBark\":true}}";

            var result = JsonSerializer.Deserialize<Container>(json);

            Assert.IsInstanceOf<Animal>(result.Pet);
            Assert.IsTrue((result.Pet as Dog)?.CanBark == true);
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<ParameterizedBase>), "Kind")]
        [KnownSubType(typeof(ParameterizedDerived), "Derived")]
        public class ParameterizedBase
        {
            public string Kind { get; set; }
            public string Name { get; set; }

            public ParameterizedBase(string name)
            {
                Name = name;
            }
        }

        public class ParameterizedDerived : ParameterizedBase
        {
            public ParameterizedDerived(string name) : base(name)
            {
            }
        }

        [Test]
        public void FallbackReadWithParameterizedConstructor()
        {
            var json = "{\"Kind\":\"Unknown\",\"Name\":\"Bob\"}";

            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ParameterizedBase>(json));

            StringAssert.Contains("parameterless constructor", exception.Message);
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<MultiPropBase>))]
        [KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
        [KnownSubTypeWithProperty(typeof(Employee), "Department")]
        [KnownSubTypeWithProperty(typeof(Artist), "Skill")]
        public class MultiPropBase
        {
            public string FirstName { get; set; }
        }

        public class Employee : MultiPropBase
        {
            public string JobTitle { get; set; }
            public string Department { get; set; }
        }

        public class Artist : MultiPropBase
        {
            public string Skill { get; set; }
        }

        [Test]
        public void MultiplePropertiesForSameSubtype()
        {
            var employee = JsonSerializer.Deserialize<MultiPropBase>(
                "{\"JobTitle\":\"Dev\",\"FirstName\":\"A\"}");

            Assert.IsInstanceOf<Employee>(employee);

            var employee2 = JsonSerializer.Deserialize<MultiPropBase>(
                "{\"Department\":\"IT\",\"FirstName\":\"B\"}");

            Assert.IsInstanceOf<Employee>(employee2);
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<AttributedBase>), "Kind")]
        [KnownSubType(typeof(AttributedSub), "Sub")]
        public class AttributedBase
        {
            public string Kind { get; set; }
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<AttributedSub>), "SubKind")]
        [KnownSubType(typeof(AttributedSubSub), "SubSub")]
        public class AttributedSub : AttributedBase
        {
            public string SubKind { get; set; }
        }

        public class AttributedSubSub : AttributedSub
        {
            public int Depth { get; set; }
        }

        [Test]
        public void ResolvedLeafWithItsOwnConverterAttribute()
        {
            var result = JsonSerializer.Deserialize<AttributedBase>(
                "{\"Kind\":\"Sub\",\"SubKind\":\"SubSub\",\"Depth\":3}");

            Assert.IsInstanceOf<AttributedSubSub>(result);
        }

        public class BuilderPerson
        {
            public string FirstName { get; set; }
        }

        public class BuilderEmployee : BuilderPerson
        {
            public string JobTitle { get; set; }
        }

        public class BuilderArtist : BuilderPerson
        {
            public string Skill { get; set; }
        }

        [Test]
        public void PropertyPresenceBuilder()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of<BuilderPerson>()
                .RegisterSubtypeWithProperty<BuilderEmployee>("JobTitle")
                .RegisterSubtypeWithProperty<BuilderArtist>("Skill")
                .Build());

            var employee = JsonSerializer.Deserialize<BuilderPerson>("{\"JobTitle\":\"Dev\"}", options);
            Assert.IsInstanceOf<BuilderEmployee>(employee);

            var artist = JsonSerializer.Deserialize<BuilderPerson>("{\"Skill\":\"Painter\"}", options);
            Assert.IsInstanceOf<BuilderArtist>(artist);
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<OpenAnimal>), "ClassName")]
        public class OpenAnimal
        {
            public string ClassName { get; set; }
        }

        public class OpenDog : OpenAnimal
        {
        }

        public class UnrelatedType
        {
            public static bool CtorCalled;

            public UnrelatedType()
            {
                CtorCalled = true;
            }
        }

        [Test]
        public void NameBasedResolutionRejectsNonSubtypes()
        {
            var animal = JsonSerializer.Deserialize<OpenAnimal>("{\"ClassName\":\"UnrelatedType\"}");

            Assert.IsInstanceOf<OpenAnimal>(animal);
            Assert.IsFalse(UnrelatedType.CtorCalled);
        }

        [Test]
        public void CrossAssemblyResolvedWhenAssemblyRegistered()
        {
            JsonSubTypesTypeResolution.ClearAssemblies();
            JsonSubTypesTypeResolution.AddAssembly(typeof(PluginDog).Assembly);
            try
            {
                var dog = JsonSerializer.Deserialize<SharedAnimal>(
                    "{\"Kind\":\"JsonSubTypes.Tests.Plugin.PluginDog\",\"CanBark\":true}");

                Assert.IsInstanceOf<PluginDog>(dog);
                Assert.IsTrue((dog as PluginDog)?.CanBark == true);
            }
            finally
            {
                JsonSubTypesTypeResolution.ClearAssemblies();
            }
        }

        [Test]
        public void CrossAssemblyNotResolvedByDefault()
        {
            JsonSubTypesTypeResolution.ClearAssemblies();
            try
            {
                var animal = JsonSerializer.Deserialize<SharedAnimal>(
                    "{\"Kind\":\"JsonSubTypes.Tests.Plugin.PluginDog\",\"CanBark\":true}");

                Assert.IsInstanceOf<SharedAnimal>(animal);
            }
            finally
            {
                JsonSubTypesTypeResolution.ClearAssemblies();
            }
        }
    }
}
