#nullable enable
using System;
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class Vehicle
    {
        public int Wheels { get; set; }
    }

    public class Car : Vehicle
    {
        public string? Brand { get; set; }
    }

    public class Bike : Vehicle
    {
        public bool HasBasket { get; set; }
    }

    public class Truck : Vehicle
    {
        public int Capacity { get; set; }
    }

    [TestFixture]
    public class JsonSubtypesResolverDivergenceTests
    {
        private static JsonSerializerOptions ConverterOptions(string discriminatorProperty)
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(
                JsonSubtypesConverterBuilder.Of<Vehicle>(discriminatorProperty)
                    .RegisterSubtype<Vehicle>("vehicle")
                    .RegisterSubtype<Car>("car")
                    .RegisterSubtype<Bike>("bike")
                    .SerializeDiscriminatorProperty()
                    .Build());
            return options;
        }

        private static JsonSerializerOptions ResolverOptions(string discriminatorProperty)
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<Vehicle>(discriminatorProperty)
                    .RegisterSubtype<Vehicle>("vehicle")
                    .RegisterSubtype<Car>("car")
                    .RegisterSubtype<Bike>("bike")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };
            return options;
        }

        [Test]
        public void NamingPolicy_IsAppliedToDiscriminatorName_ByConverter_ButNotByNative()
        {
            var converterOptions = ConverterOptions("Kind");
            converterOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var resolverOptions = ResolverOptions("Kind");
            resolverOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            string converterJson = JsonSerializer.Serialize<Vehicle>(new Car { Brand = "VW" }, converterOptions);
            string resolverJson = JsonSerializer.Serialize<Vehicle>(new Car { Brand = "VW" }, resolverOptions);

            StringAssert.StartsWith("{\"kind\":\"car\"", converterJson);
            StringAssert.StartsWith("{\"Kind\":\"car\"", resolverJson);
        }

        [Test]
        public void BaseInstanceRegistered_WritesSameJsonInBothModes()
        {
            string converterJson = JsonSerializer.Serialize<Vehicle>(new Vehicle { Wheels = 3 }, ConverterOptions("$type"));
            string resolverJson = JsonSerializer.Serialize<Vehicle>(new Vehicle { Wheels = 3 }, ResolverOptions("$type"));

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("{\"$type\":\"vehicle\",\"Wheels\":3}", resolverJson);
        }

        [Test]
        public void BaseInstanceNotRegistered_ConverterThrows_NativeWritesPlainObject()
        {
            var converterOptions = new JsonSerializerOptions();
            converterOptions.Converters.Add(
                JsonSubtypesConverterBuilder.Of<Vehicle>("$type")
                    .RegisterSubtype<Car>("car")
                    .RegisterSubtype<Bike>("bike")
                    .SerializeDiscriminatorProperty()
                    .Build());
            var resolverOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<Vehicle>("$type")
                    .RegisterSubtype<Car>("car")
                    .RegisterSubtype<Bike>("bike")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };

            Assert.Throws<JsonException>(
                () => JsonSerializer.Serialize<Vehicle>(new Vehicle { Wheels = 3 }, converterOptions));

            string resolverJson = JsonSerializer.Serialize<Vehicle>(new Vehicle { Wheels = 3 }, resolverOptions);
            Assert.AreEqual("{\"Wheels\":3}", resolverJson);
        }

        [Test]
        public void CaseInsensitiveDiscriminatorName_ConverterResolves_NativeFallsBackToBase()
        {
            const string json = "{\"kind\":\"car\",\"Brand\":\"VW\",\"Wheels\":4}";

            var converterOptions = ConverterOptions("Kind");
            converterOptions.PropertyNameCaseInsensitive = true;
            var resolverOptions = ResolverOptions("Kind");
            resolverOptions.PropertyNameCaseInsensitive = true;

            Vehicle? fromConverter = JsonSerializer.Deserialize<Vehicle>(json, converterOptions);
            Vehicle? fromResolver = JsonSerializer.Deserialize<Vehicle>(json, resolverOptions);

            Assert.IsInstanceOf<Car>(fromConverter);
            Assert.IsInstanceOf<Vehicle>(fromResolver);
        }

        [Test]
        public void CaseInsensitiveDollarDiscriminator_ConverterResolves_NativeThrows()
        {
            const string json = "{\"$TYPE\":\"car\",\"Brand\":\"VW\",\"Wheels\":4}";

            var converterOptions = ConverterOptions("$type");
            converterOptions.PropertyNameCaseInsensitive = true;
            var resolverOptions = ResolverOptions("$type");
            resolverOptions.PropertyNameCaseInsensitive = true;

            Vehicle? fromConverter = JsonSerializer.Deserialize<Vehicle>(json, converterOptions);
            Assert.IsInstanceOf<Car>(fromConverter);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Vehicle>(json, resolverOptions));
        }

        [Test]
        public void MissingDiscriminatorOnInterface_ConverterThrowsJsonException_NativeThrowsNotSupported()
        {
            const string json = "{\"Radius\":10}";

            var converterOptions = new JsonSerializerOptions();
            converterOptions.Converters.Add(
                JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .SerializeDiscriminatorProperty()
                    .Build());
            var resolverOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IShape>(json, converterOptions));

            var exception = Assert.Throws<NotSupportedException>(
                () => JsonSerializer.Deserialize<IShape>(json, resolverOptions));
            StringAssert.Contains("IShape", exception?.Message);
        }

        [Test]
        public void UnregisteredDerivedType_BothThrow_DifferentExceptions()
        {
            var converterOptions = new JsonSerializerOptions();
            converterOptions.Converters.Add(
                JsonSubtypesConverterBuilder.Of<Vehicle>("$type")
                    .RegisterSubtype<Car>("car")
                    .RegisterSubtype<Bike>("bike")
                    .SerializeDiscriminatorProperty()
                    .Build());
            var resolverOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<Vehicle>("$type")
                    .RegisterSubtype<Car>("car")
                    .RegisterSubtype<Bike>("bike")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };
            var truck = new Truck { Capacity = 10 };

            Assert.Throws<JsonException>(
                () => JsonSerializer.Serialize<Vehicle>(truck, converterOptions));

            var exception = Assert.Throws<NotSupportedException>(
                () => JsonSerializer.Serialize<Vehicle>(truck, resolverOptions));
            StringAssert.Contains("Truck", exception?.Message);
        }

        [Test]
        public void TypeInfoResolverChain_DoesNotComposeSeveralNativeResolvers()
        {
            var options = new JsonSerializerOptions();
            options.TypeInfoResolverChain.Add(
                JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver());
            options.TypeInfoResolverChain.Add(
                JsonSubtypesConverterBuilder.Of<IAnimal>("kind")
                    .RegisterSubtype<Dog>("dog")
                    .RegisterSubtype<Cat>("cat")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver());

            string shapeJson = JsonSerializer.Serialize<IShape>(new Circle { Radius = 2 }, options);
            string animalJson = JsonSerializer.Serialize<IAnimal>(new Dog { Name = "Rex" }, options);

            Assert.AreEqual("{\"$type\":\"circle\",\"Radius\":2}", shapeJson);
            Assert.AreNotEqual("{\"kind\":\"dog\",\"Name\":\"Rex\"}", animalJson);
        }
    }
}
