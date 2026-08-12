#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public interface IShape
    {
    }

    public class Circle : IShape
    {
        public double Radius { get; set; }
    }

    public class Square : IShape
    {
        public double Length { get; set; }
    }

    public class Box
    {
        public IShape? Something { get; set; }
    }

    public class PlainPoco
    {
        public string? Name { get; set; }
    }

    [TestFixture]
    public class JsonSubtypesResolverParityTests
    {
        private static JsonSerializerOptions ConverterOptions(string discriminatorProperty = "$type")
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(
                JsonSubtypesConverterBuilder.Of<IShape>(discriminatorProperty)
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .SerializeDiscriminatorProperty()
                    .Build());
            return options;
        }

        private static JsonSerializerOptions ResolverOptions(string discriminatorProperty = "$type")
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<IShape>(discriminatorProperty)
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };
            return options;
        }

        [Test]
        public void SerializeDerivedType_WritesSameJsonAsConverter()
        {
            var shape = new Circle { Radius = 10 };

            string converterJson = JsonSerializer.Serialize<IShape>(shape, ConverterOptions());
            string resolverJson = JsonSerializer.Serialize<IShape>(shape, ResolverOptions());

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("{\"$type\":\"circle\",\"Radius\":10}", resolverJson);
        }

        [Test]
        public void SerializeEverySubtype_WritesSameJsonAsConverter()
        {
            IShape[] shapes = [new Circle { Radius = 10 }, new Square { Length = 4 }];

            foreach (IShape shape in shapes)
            {
                string converterJson = JsonSerializer.Serialize(shape, ConverterOptions());
                string resolverJson = JsonSerializer.Serialize(shape, ResolverOptions());

                Assert.AreEqual(converterJson, resolverJson);
            }
        }

        [Test]
        public void Deserialize_ReturnsSameTypeAsConverter()
        {
            const string json = "{\"$type\":\"circle\",\"Radius\":10}";

            IShape? fromConverter = JsonSerializer.Deserialize<IShape>(json, ConverterOptions());
            IShape? fromResolver = JsonSerializer.Deserialize<IShape>(json, ResolverOptions());

            Assert.AreEqual(fromConverter?.GetType(), fromResolver?.GetType());
            Assert.IsInstanceOf<Circle>(fromResolver);
        }

        [Test]
        public void Deserialize_EverySubtype_ReturnsSameTypeAsConverter()
        {
            string[] jsons = ["{\"$type\":\"circle\",\"Radius\":10}", "{\"$type\":\"square\",\"Length\":4}"];

            foreach (string json in jsons)
            {
                IShape? fromConverter = JsonSerializer.Deserialize<IShape>(json, ConverterOptions());
                IShape? fromResolver = JsonSerializer.Deserialize<IShape>(json, ResolverOptions());

                Assert.AreEqual(fromConverter?.GetType(), fromResolver?.GetType());
            }
        }

        [Test]
        public void RoundTrip_ProducesOriginalValues()
        {
            var original = new Circle { Radius = 3.5 };

            string json = JsonSerializer.Serialize<IShape>(original, ResolverOptions());
            var back = JsonSerializer.Deserialize<IShape>(json, ResolverOptions());

            Assert.IsInstanceOf<Circle>(back);
            Assert.AreEqual(3.5, ((Circle)back!).Radius);
        }

        [Test]
        public void DeserializeWithConverterJson_AndViceVersa_YieldSameResult()
        {
            var original = new Square { Length = 8 };

            string converterJson = JsonSerializer.Serialize<IShape>(original, ConverterOptions());
            string resolverJson = JsonSerializer.Serialize<IShape>(original, ResolverOptions());

            IShape? a = JsonSerializer.Deserialize<IShape>(resolverJson, ConverterOptions());
            IShape? b = JsonSerializer.Deserialize<IShape>(converterJson, ResolverOptions());

            Assert.AreEqual(a?.GetType(), b?.GetType());
        }

        [Test]
        public void SerializeAndDeserialize_Collection_WritesSameJsonAsConverter()
        {
            var shapes = new List<IShape> { new Circle { Radius = 1 }, new Square { Length = 2 } };

            string converterJson = JsonSerializer.Serialize(shapes, ConverterOptions());
            string resolverJson = JsonSerializer.Serialize(shapes, ResolverOptions());

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual(
                "[{\"$type\":\"circle\",\"Radius\":1},{\"$type\":\"square\",\"Length\":2}]",
                resolverJson);

            var back = JsonSerializer.Deserialize<List<IShape>>(resolverJson, ResolverOptions());
            Assert.AreEqual(2, back?.Count);
            Assert.IsInstanceOf<Circle>(back?[0]);
            Assert.IsInstanceOf<Square>(back?[1]);
        }

        [Test]
        public void SerializeAndDeserialize_Array_WritesSameJsonAsConverter()
        {
            IShape[] shapes = [new Circle { Radius = 1 }];

            string converterJson = JsonSerializer.Serialize(shapes, ConverterOptions());
            string resolverJson = JsonSerializer.Serialize(shapes, ResolverOptions());

            Assert.AreEqual(converterJson, resolverJson);

            var back = JsonSerializer.Deserialize<IShape[]>(resolverJson, ResolverOptions());
            Assert.IsInstanceOf<Circle>(back?[0]);
        }

        [Test]
        public void SerializeAndDeserialize_NestedProperty_WritesSameJsonAsConverter()
        {
            var box = new Box { Something = new Circle { Radius = 5 } };

            string converterJson = JsonSerializer.Serialize(box, ConverterOptions());
            string resolverJson = JsonSerializer.Serialize(box, ResolverOptions());

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("{\"Something\":{\"$type\":\"circle\",\"Radius\":5}}", resolverJson);

            var back = JsonSerializer.Deserialize<Box>(resolverJson, ResolverOptions());
            Assert.IsInstanceOf<Circle>(back?.Something);
        }

        [Test]
        public void CustomDiscriminatorPropertyName_WritesSameJsonAsConverter()
        {
            var shape = new Circle { Radius = 10 };

            string converterJson = JsonSerializer.Serialize<IShape>(shape, ConverterOptions("kind"));
            string resolverJson = JsonSerializer.Serialize<IShape>(shape, ResolverOptions("kind"));

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("{\"kind\":\"circle\",\"Radius\":10}", resolverJson);
        }

        [Test]
        public void NamingPolicy_WritesSameJsonAsConverter_WhenDiscriminatorUnaffected()
        {
            var shape = new Circle { Radius = 10 };
            var converterOptions = ConverterOptions();
            converterOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var resolverOptions = ResolverOptions();
            resolverOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            string converterJson = JsonSerializer.Serialize<IShape>(shape, converterOptions);
            string resolverJson = JsonSerializer.Serialize<IShape>(shape, resolverOptions);

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("{\"$type\":\"circle\",\"radius\":10}", resolverJson);
        }

        [Test]
        public void IntDiscriminator_WritesSameJsonAsConverter()
        {
            var square = new Square { Length = 4 };
            var converterOptions = new JsonSerializerOptions();
            converterOptions.Converters.Add(
                JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>(42)
                    .SerializeDiscriminatorProperty()
                    .Build());
            var resolverOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>(42)
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };

            string converterJson = JsonSerializer.Serialize<IShape>(square, converterOptions);
            string resolverJson = JsonSerializer.Serialize<IShape>(square, resolverOptions);

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("{\"$type\":42,\"Length\":4}", resolverJson);

            IShape? back = JsonSerializer.Deserialize<IShape>("{\"$type\":42,\"Length\":4}", resolverOptions);
            Assert.IsInstanceOf<Square>(back);
        }

        [Test]
        public void SerializeNull_And_DeserializeNull_MatchConverter()
        {
            IShape? nullShape = null;
            string converterJson = JsonSerializer.Serialize(nullShape, ConverterOptions());
            string resolverJson = JsonSerializer.Serialize(nullShape, ResolverOptions());

            Assert.AreEqual(converterJson, resolverJson);
            Assert.AreEqual("null", resolverJson);

            IShape? back = JsonSerializer.Deserialize<IShape>("null", ResolverOptions());
            Assert.IsNull(back);
        }

        [Test]
        public void MissingDiscriminator_AbstractBase_ThrowsInBoth()
        {
            const string json = "{\"Radius\":10}";

            Assert.Catch(() => JsonSerializer.Deserialize<IShape>(json, ConverterOptions()));
            Assert.Catch(() => JsonSerializer.Deserialize<IShape>(json, ResolverOptions()));
        }

        [Test]
        public void UnknownDiscriminator_ThrowsInBoth()
        {
            const string json = "{\"$type\":\"triangle\",\"Radius\":10}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IShape>(json, ConverterOptions()));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IShape>(json, ResolverOptions()));
        }

        [Test]
        public void NonPolymorphicType_IsUnaffected()
        {
            var options = ResolverOptions();

            string json = JsonSerializer.Serialize(new PlainPoco { Name = "hello" }, options);

            Assert.AreEqual("{\"Name\":\"hello\"}", json);
        }

        [Test]
        public void NativeResolver_CoexistsWithConverter_InSameOptions()
        {
            IAnimal animal = new Dog { Name = "Rex" };
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .SerializeDiscriminatorProperty()
                    .BuildResolver()
            };
            options.Converters.Add(
                JsonSubtypesConverterBuilder.Of<IAnimal>("kind")
                    .RegisterSubtype<Dog>("dog")
                    .RegisterSubtype<Cat>("cat")
                    .SerializeDiscriminatorProperty()
                    .Build());

            string shapeJson = JsonSerializer.Serialize<IShape>(new Circle { Radius = 2 }, options);
            string animalJson = JsonSerializer.Serialize<IAnimal>(animal, options);

            Assert.AreEqual("{\"$type\":\"circle\",\"Radius\":2}", shapeJson);
            Assert.AreEqual("{\"kind\":\"dog\",\"Name\":\"Rex\"}", animalJson);

            Assert.IsInstanceOf<Circle>(JsonSerializer.Deserialize<IShape>(shapeJson, options));
            Assert.IsInstanceOf<Dog>(JsonSerializer.Deserialize<IAnimal>(animalJson, options));
        }

        [Test]
        public void BuildResolver_ReturnsPublicJsonSubtypesResolverType()
        {
            JsonSubtypesResolver resolver = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>("circle")
                .RegisterSubtype<Square>("square")
                .SerializeDiscriminatorProperty()
                .BuildResolver();

            Assert.IsNotNull(resolver);

            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
            Assert.AreEqual(
                "{\"$type\":\"circle\",\"Radius\":1}",
                JsonSerializer.Serialize<IShape>(new Circle { Radius = 1 }, options));
        }

        [Test]
        public void TwoIndependentResolvers_DoNotInterfere()
        {
            var first = ResolverOptions();
            var second = ResolverOptions();
            second.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            string a = JsonSerializer.Serialize<IShape>(new Circle { Radius = 1 }, first);
            string b = JsonSerializer.Serialize<IShape>(new Circle { Radius = 1 }, second);

            Assert.AreEqual("{\"$type\":\"circle\",\"Radius\":1}", a);
            Assert.AreEqual("{\"$type\":\"circle\",\"radius\":1}", b);
        }

        [Test]
        public void BuildResolvers_CombinesSeveralHierarchies()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonSubtypesConverterBuilder.BuildResolvers(
                    JsonSubtypesConverterBuilder.Of<IShape>("$type")
                        .RegisterSubtype<Circle>("circle")
                        .RegisterSubtype<Square>("square")
                        .SerializeDiscriminatorProperty(),
                    JsonSubtypesConverterBuilder.Of<IAnimal>("kind")
                        .RegisterSubtype<Dog>("dog")
                        .RegisterSubtype<Cat>("cat")
                        .SerializeDiscriminatorProperty())
            };

            string shapeJson = JsonSerializer.Serialize<IShape>(new Circle { Radius = 2 }, options);
            string animalJson = JsonSerializer.Serialize<IAnimal>(new Dog { Name = "Rex" }, options);

            Assert.AreEqual("{\"$type\":\"circle\",\"Radius\":2}", shapeJson);
            Assert.AreEqual("{\"kind\":\"dog\",\"Name\":\"Rex\"}", animalJson);

            Assert.IsInstanceOf<Circle>(JsonSerializer.Deserialize<IShape>(shapeJson, options));
            Assert.IsInstanceOf<Dog>(JsonSerializer.Deserialize<IAnimal>(animalJson, options));
        }

        [Test]
        public void BuildResolvers_WithNoBuilder_Throws()
        {
            Assert.Throws<ArgumentException>(
                () => JsonSubtypesConverterBuilder.BuildResolvers());
        }

        [Test]
        public void BuildResolvers_WithDuplicateBaseType_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                JsonSubtypesConverterBuilder.BuildResolvers(
                    JsonSubtypesConverterBuilder.Of<IShape>("$type")
                        .RegisterSubtype<Circle>("circle")
                        .SerializeDiscriminatorProperty(),
                    JsonSubtypesConverterBuilder.Of<IShape>("kind")
                        .RegisterSubtype<Square>("square")
                        .SerializeDiscriminatorProperty()));

            StringAssert.Contains("Several builders target the same base type", exception?.Message);
        }
    }

    public interface IAnimal
    {
    }

    public class Dog : IAnimal
    {
        public string? Name { get; set; }
    }

    public class Cat : IAnimal
    {
        public int Lives { get; set; }
    }
}
