#nullable enable
using System;
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class JsonSubtypesResolverNativeOptionsTests
    {
        private static JsonSerializerOptions Options(JsonSubtypesResolver resolver)
        {
            return new JsonSerializerOptions { TypeInfoResolver = resolver };
        }

        // ---- FallBackToNearestAncestor ----

        [Test]
        public void FallBackToNearestAncestor_SerializesUnregisteredDerivedAsNearestAncestor()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<IWidget>("$type")
                .RegisterSubtype<Widget>("widget")
                .RegisterSubtype<RoundWidget>("round")
                .SerializeDiscriminatorProperty()
                .FallBackToNearestAncestor()
                .BuildResolver());

            string json = JsonSerializer.Serialize<IWidget>(new FancyRoundWidget { Diameter = 4, Finish = "matte" }, options);

            Assert.AreEqual("{\"$type\":\"round\",\"Diameter\":4,\"Name\":null}", json);
        }

        [Test]
        public void FallBackToNearestAncestor_RegisteredTypesAreUnchanged()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<IWidget>("$type")
                .RegisterSubtype<Widget>("widget")
                .RegisterSubtype<RoundWidget>("round")
                .SerializeDiscriminatorProperty()
                .FallBackToNearestAncestor()
                .BuildResolver());

            string json = JsonSerializer.Serialize<IWidget>(new RoundWidget { Diameter = 4 }, options);

            Assert.AreEqual("{\"$type\":\"round\",\"Diameter\":4,\"Name\":null}", json);
        }

        [Test]
        public void WithoutFallBackToNearestAncestor_UnregisteredDerivedThrows()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<IWidget>("$type")
                .RegisterSubtype<Widget>("widget")
                .RegisterSubtype<RoundWidget>("round")
                .SerializeDiscriminatorProperty()
                .BuildResolver());

            Assert.Throws<NotSupportedException>(
                () => JsonSerializer.Serialize<IWidget>(new FancyRoundWidget { Diameter = 4 }, options));
        }

        [Test]
        public void Build_WithFallBackToNearestAncestor_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IWidget>("$type")
                .RegisterSubtype<RoundWidget>("round")
                .SerializeDiscriminatorProperty()
                .FallBackToNearestAncestor();

            var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
            StringAssert.Contains("only supported by BuildResolver", exception?.Message);
        }

        // ---- IgnoreUnrecognizedTypeDiscriminators ----

        [Test]
        public void IgnoreUnrecognizedTypeDiscriminators_FallsBackToBaseOnUnknownDiscriminator()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .RegisterSubtype<CarV>("car")
                .SerializeDiscriminatorProperty()
                .IgnoreUnrecognizedTypeDiscriminators()
                .BuildResolver());

            VehicleBase? result = JsonSerializer.Deserialize<VehicleBase>("{\"$type\":\"truck\",\"Wheels\":4}", options);

            Assert.IsInstanceOf<VehicleBase>(result);
        }

        [Test]
        public void WithoutIgnoreUnrecognized_UnknownDiscriminatorThrows()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .RegisterSubtype<CarV>("car")
                .SerializeDiscriminatorProperty()
                .BuildResolver());

            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<VehicleBase>("{\"$type\":\"truck\",\"Wheels\":4}", options));
        }

        [Test]
        public void Build_WithIgnoreUnrecognizedTypeDiscriminators_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .RegisterSubtype<CarV>("car")
                .SerializeDiscriminatorProperty()
                .IgnoreUnrecognizedTypeDiscriminators();

            var exception = Assert.Throws<NotSupportedException>(() => builder.Build());
            StringAssert.Contains("only supported by BuildResolver", exception?.Message);
        }

        // ---- SetFallbackSubtype(base) maps to IgnoreUnrecognizedTypeDiscriminators ----

        [Test]
        public void SetFallbackSubtypeBase_BehavesLikeIgnoreUnrecognized_AndMatchesConverter()
        {
            var resolverOptions = Options(JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .RegisterSubtype<CarV>("car")
                .SerializeDiscriminatorProperty()
                .SetFallbackSubtype<VehicleBase>()
                .BuildResolver());

            var converterOptions = new JsonSerializerOptions();
            converterOptions.Converters.Add(JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .RegisterSubtype<CarV>("car")
                .SerializeDiscriminatorProperty()
                .SetFallbackSubtype<VehicleBase>()
                .Build());

            const string json = "{\"$type\":\"truck\",\"Wheels\":4}";
            VehicleBase? fromResolver = JsonSerializer.Deserialize<VehicleBase>(json, resolverOptions);
            VehicleBase? fromConverter = JsonSerializer.Deserialize<VehicleBase>(json, converterOptions);

            Assert.IsInstanceOf<VehicleBase>(fromResolver);
            Assert.AreEqual(fromConverter?.GetType(), fromResolver?.GetType());
        }

        [Test]
        public void SetFallbackSubtypeNonBase_StillThrows()
        {
            var builder = JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .RegisterSubtype<CarV>("car")
                .RegisterSubtype<BikeV>("bike")
                .SerializeDiscriminatorProperty()
                .SetFallbackSubtype<BikeV>();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());
            StringAssert.Contains("SetFallbackSubtype is not supported", exception?.Message);
        }

        // ---- Attribute mode ----

        [Test]
        public void KnownSubTypeAttributes_AreUsedWhenNothingRegisteredExplicitly()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<AttrVehicle>("$type")
                .SerializeDiscriminatorProperty()
                .BuildResolver());

            string json = JsonSerializer.Serialize<AttrVehicle>(new AttrCar { Brand = "VW", Wheels = 4 }, options);
            Assert.AreEqual("{\"$type\":\"car\",\"Brand\":\"VW\",\"Wheels\":4}", json);

            var back = JsonSerializer.Deserialize<AttrVehicle>(json, options);
            Assert.IsInstanceOf<AttrCar>(back);
        }

        [Test]
        public void KnownSubTypeAttributes_WithEnumDiscriminator_Throw()
        {
            var builder = JsonSubtypesConverterBuilder.Of<AttrShapeBase>("$type")
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());
            StringAssert.Contains("only string and int are supported", exception?.Message);
        }

        [Test]
        public void FallBackSubTypeAttributeEqualToBase_IsHonored()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<FbVehicle>("$type")
                .SerializeDiscriminatorProperty()
                .BuildResolver());

            FbVehicle? result = JsonSerializer.Deserialize<FbVehicle>("{\"$type\":\"unknown\",\"Wheels\":2}", options);

            Assert.IsInstanceOf<FbVehicle>(result);
        }

        [Test]
        public void FallBackSubTypeAttributeNonBase_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<CbVehicle>("$type")
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());
            StringAssert.Contains("SetFallbackSubtype is not supported", exception?.Message);
        }

        [Test]
        public void NoRegistrationAndNoAttributes_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<VehicleBase>("$type")
                .SerializeDiscriminatorProperty();

            Assert.Throws<InvalidOperationException>(() => builder.BuildResolver());
        }

        // ---- Collision pitfall ----

        [Test]
        public void DiscriminatorNameCollidingWithPropertyName_BehavesPerFramework()
        {
            var options = Options(JsonSubtypesConverterBuilder.Of<CollidingShape>("Kind")
                .RegisterSubtype<CollidingCircle>("circle")
                .SerializeDiscriminatorProperty()
                .BuildResolver());

#if NET8_0
            string json = JsonSerializer.Serialize<CollidingShape>(new CollidingCircle { Radius = 1 }, options);
            StringAssert.Contains("\"Kind\":\"circle\"", json);
#else
            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize<CollidingShape>(new CollidingCircle { Radius = 1 }, options));
#endif
        }
    }

    // ---- domain types ----

    public interface IWidget
    {
    }

    public class Widget : IWidget
    {
        public string? Name { get; set; }
    }

    public class RoundWidget : Widget
    {
        public double Diameter { get; set; }
    }

    public class FancyRoundWidget : RoundWidget
    {
        public string? Finish { get; set; }
    }

    public class VehicleBase
    {
        public int Wheels { get; set; }
    }

    public class CarV : VehicleBase
    {
        public string? Brand { get; set; }
    }

    public class BikeV : VehicleBase
    {
        public bool HasBasket { get; set; }
    }

    [KnownSubType(typeof(AttrCar), "car")]
    [KnownSubType(typeof(AttrBike), "bike")]
    public class AttrVehicle
    {
        public int Wheels { get; set; }
    }

    public class AttrCar : AttrVehicle
    {
        public string? Brand { get; set; }
    }

    public class AttrBike : AttrVehicle
    {
        public bool HasBasket { get; set; }
    }

    public enum AttrShapeKind
    {
        Circle,
        Square
    }

    [KnownSubType(typeof(AttrCircle), AttrShapeKind.Circle)]
    public class AttrShapeBase
    {
    }

    public class AttrCircle : AttrShapeBase
    {
    }

    [KnownSubType(typeof(FbCar), "car")]
    [FallBackSubType(typeof(FbVehicle))]
    public class FbVehicle
    {
        public int Wheels { get; set; }
    }

    public class FbCar : FbVehicle
    {
        public string? Brand { get; set; }
    }

    [KnownSubType(typeof(CbCar), "car")]
    [FallBackSubType(typeof(CbBike))]
    public class CbVehicle
    {
    }

    public class CbCar : CbVehicle
    {
    }

    public class CbBike : CbVehicle
    {
    }

    public class CollidingShape
    {
        public string? Kind { get; set; }
    }

    public class CollidingCircle : CollidingShape
    {
        public double Radius { get; set; }
    }
}
