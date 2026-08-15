using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class DynamicRegisterTests
    {
        public abstract class Animal
        {
            [JsonPropertyName("age")]
            public int Age { get; set; }
        }

        public class Dog : Animal
        {
            public bool CanBark { get; set; } = true;
        }

        public class Cat : Animal
        {
            [JsonPropertyName("catLives")]
            public int Lives { get; set; } = 7;
        }

        public abstract class Fish : Animal
        {
            [JsonPropertyName("fins")]
            public uint FinCount { get; set; }
        }

        public class Shark : Fish
        {
            [JsonPropertyName("teethRows")]
            public uint TeethRows { get; set; }
        }

        public class HammerheadShark : Shark
        {
            [JsonPropertyName("hammerSize")]
            public float HammerSize { get; set; }
        }

        public enum AnimalType
        {
            Dog = 1,
            Cat = 2,
            Shark = 3,
            HammerheadShark = 4
        }

        [Test]
        public void DeserializeTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"type\":2,\"age\":11}";

            var result = JsonSerializer.Deserialize<Animal>(json, options);

            Assert.AreEqual(typeof(Cat), result.GetType());
            Assert.AreEqual(11, result.Age);
            Assert.AreEqual(6, (result as Cat)?.Lives);
        }

        [Test]
        public void DeserializeIncompleteTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"type\":2}";

            var result = JsonSerializer.Deserialize<Animal>(json, options);

            Assert.AreEqual(typeof(Cat), result.GetType());
            Assert.AreEqual(0, result.Age);
            Assert.AreEqual(7, (result as Cat)?.Lives);
        }

        [Test]
        public void UnregisteredTypeDeserializeTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                .Build());

            var json = "{\"age\":11,\"fins\":4,\"teethRows\":4,\"hammerSize\":42.1,\"type\":4}";

            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Animal>(json, options));

            Assert.IsTrue(exception.Message.Contains("Type is an interface or abstract class and cannot be instantiated."));
        }

        [Test]
        public void NestedTypeDeserializeTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(HammerheadShark), AnimalType.HammerheadShark)
                .Build());

            var json = "{\"age\":11,\"fins\":3,\"teethRows\":4,\"hammerSize\":42.1,\"type\":4}";

            var result = JsonSerializer.Deserialize<Animal>(json, options);

            Assert.AreEqual(typeof(HammerheadShark), result.GetType());
            Assert.AreEqual(11, result.Age);
            Assert.AreEqual(3u, (result as Fish)?.FinCount);
            Assert.AreEqual(4u, (result as Shark)?.TeethRows);
            Assert.AreEqual(42.1f, (result as HammerheadShark)?.HammerSize);
        }

        [Test]
        public void ItRefuseToRegisterTwiceWithTheSameValue()
        {
            var jsonSubtypesConverterBuilder = JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat);

            Assert.Throws<ArgumentException>(() => jsonSubtypesConverterBuilder.RegisterSubtype(typeof(Dog), AnimalType.Cat));
        }

        [Test]
        public void ItRefuseToRegisterTwiceWithTheSameNullValue()
        {
            var jsonSubtypesConverterBuilder = JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), null);

            Assert.Throws<ArgumentException>(() => jsonSubtypesConverterBuilder.RegisterSubtype(typeof(Dog), null));
        }

        [Test]
        public void SerializeTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"type\":2,\"catLives\":6,\"age\":11}";

            var result = JsonSerializer.Serialize<Animal>(new Cat { Age = 11, Lives = 6 }, options);

            Assert.AreEqual(json, result);
        }

        [Test]
        public void RegisterWithGenericTypes()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of<Animal>("type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype<Cat>(AnimalType.Cat)
                .RegisterSubtype<Dog>(AnimalType.Dog)
                .Build());

            var json = "{\"type\":2,\"catLives\":6,\"age\":11}";

            var result = JsonSerializer.Serialize<Animal>(new Cat { Age = 11, Lives = 6 }, options);

            Assert.AreEqual(json, result);
        }

        [Test]
        public void SerializeTestDiscriminatorLast()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty(false)
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .Build());

            var json = "{\"catLives\":6,\"age\":11,\"type\":2}";

            var result = JsonSerializer.Serialize<Animal>(new Cat { Age = 11, Lives = 6 }, options);

            Assert.AreEqual(json, result);
        }

        [Test]
        public void MultipleRegistrationNotAllowedWithSerializeDiscriminatorProperty()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                .RegisterSubtype(typeof(Shark), AnimalType.HammerheadShark)
                .Build());

            Assert.AreEqual("Multiple discriminators on single type are not supported when discriminator serialization is enabled", exception.Message);
        }

        [Test]
        public void UnregisteredTypeSerializeTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(Shark), AnimalType.Shark)
                .Build());

            var json = "{\"hammerSize\":42.1,\"teethRows\":4,\"fins\":4,\"age\":11}";

            var result = JsonSerializer.Serialize<Animal>(new HammerheadShark
            {
                Age = 11,
                FinCount = 4,
                HammerSize = 42.1f,
                TeethRows = 4
            }, options);

            Assert.AreEqual(json, result);
        }

        [Test]
        public void UnregisteredTypeSerializeTest2()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), AnimalType.Cat)
                .RegisterSubtype(typeof(Dog), AnimalType.Dog)
                .RegisterSubtype(typeof(HammerheadShark), AnimalType.HammerheadShark)
                .Build());

            var json = "{\"teethRows\":4,\"fins\":4,\"age\":11}";

            var result = JsonSerializer.Serialize<Animal>(new Shark
            {
                Age = 11,
                FinCount = 4,
                TeethRows = 4
            }, options);

            Assert.AreEqual(json, result);
        }

        public class Cat2 : Animal2
        {
        }

        public class Animal2
        {
        }

        [Test]
        public void ExplicitExceptionWhenMappingNotRegistered()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Animal2), "Type")
                .SerializeDiscriminatorProperty()
                .Build());

            var e1 = Assert.Throws<JsonException>(() => JsonSerializer.Serialize<Animal2>(new Animal2(), options));
            Assert.AreEqual("Impossible to serialize type: JsonSubTypes.Tests.DynamicRegisterTests+Animal2 because there is no registered mapping for the discriminator property", e1.Message);
            var e2 = Assert.Throws<JsonException>(() => JsonSerializer.Serialize<Animal2>(new Cat2(), options));
            Assert.AreEqual("Impossible to serialize type: JsonSubTypes.Tests.DynamicRegisterTests+Cat2 because there is no registered mapping for the discriminator property", e2.Message);
        }

        public interface IExpression
        {
            string Type { get; }
        }

        public class BinaryExpression : IExpression
        {
            public IExpression SubExpressionA { get; set; }
            public IExpression SubExpressionB { get; set; }
            public string Type { get { return "Binary"; } }
        }

        public class ConstantExpression : IExpression
        {
            private string _type = "Constant";

            public string Value { get; set; }

            public string Type
            {
                get => _type;
                set => _type = value;
            }
        }

        [Test]
        public void MultipleRegistrationAllowed()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(ConstantExpression), "StringConstant")
                .Build());

            var constant = JsonSerializer.Deserialize<IExpression>("{\"Type\":\"Constant\",\"Value\":\"B\"}", options);
            var stringConstant = JsonSerializer.Deserialize<IExpression>("{\"Type\":\"StringConstant\",\"Value\":\"C\"}", options);

            Assert.AreEqual(typeof(ConstantExpression), constant.GetType());
            Assert.AreEqual(typeof(ConstantExpression), stringConstant.GetType());
            Assert.AreEqual("Constant", constant.Type);
            Assert.AreEqual("StringConstant", stringConstant.Type);
            Assert.AreEqual("B", ((ConstantExpression)constant).Value);
            Assert.AreEqual("C", ((ConstantExpression)stringConstant).Value);
        }

        public class NullExpression : IExpression
        {
            public bool Last { get; set; }
            public string Type { get { return null; } }
        }

        public class UnknownExpression : IExpression
        {
            public string Type { get; set; }
        }

        [Test]
        public void TestIfNullIsDeserialized()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(NullExpression), null)
                .Build());

            var expr = JsonSerializer.Deserialize<IExpression>("{\"Type\": null,\"Last\":true}", options);

            Assert.AreEqual(true, (expr as NullExpression)?.Last);
        }

        [Test]
        public void TestIfNullIsDeserializedWhenFallbackDefined()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .SetFallbackSubtype(typeof(UnknownExpression))
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(NullExpression), null)
                .Build());

            var expr = JsonSerializer.Deserialize<IExpression>("{\"Type\": null,\"Last\":true}", options);

            Assert.AreEqual(true, (expr as NullExpression)?.Last);
        }

        [Test]
        public void TestFallBack()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .SetFallbackSubtype(typeof(UnknownExpression))
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .Build());

            var expr = JsonSerializer.Deserialize<IExpression>("{\"Type\": \"False\"}", options);

            Assert.AreEqual("False", (expr as UnknownExpression)?.Type);
        }

        [Test]
        public void TestFallBackGeneric()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .SetFallbackSubtype<UnknownExpression>()
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .Build());

            var expr = JsonSerializer.Deserialize<IExpression>("{\"Type\": \"False\"}", options);

            Assert.AreEqual("False", (expr as UnknownExpression)?.Type);
        }

        [Test]
        public void TestFallBackWithNullRegistered()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .SetFallbackSubtype(typeof(UnknownExpression))
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(NullExpression), null)
                .Build());

            var expr = JsonSerializer.Deserialize<IExpression>("{\"Type\": \"False\"}", options);

            Assert.AreEqual("False", (expr as UnknownExpression)?.Type);
        }

        [Test]
        public void TestIfNestedObjectIsDeserialized()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(BinaryExpression), "Binary")
                .Build());

            var binary = JsonSerializer.Deserialize<IExpression>("{\"Type\":\"Binary\"," +
                                                                "\"SubExpressionA\":{\"Type\":\"Constant\",\"Value\":\"A\"}," +
                                                                "\"SubExpressionB\":{\"Type\":\"Constant\",\"Value\":\"B\"}" +
                                                                "}", options);

            Assert.AreEqual(typeof(ConstantExpression), (binary as BinaryExpression)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression), "Type")
                .RegisterSubtype(typeof(ConstantExpression), "Constant")
                .RegisterSubtype(typeof(BinaryExpression), "Binary")
                .Build());

            var json = JsonSerializer.Serialize(new BinaryExpression
            {
                SubExpressionA = new ConstantExpression { Value = "A" },
                SubExpressionB = new ConstantExpression { Value = "B" }
            }, options);

            Assert.AreEqual("{" +
                            "\"SubExpressionA\":{\"Value\":\"A\",\"Type\":\"Constant\"}," +
                            "\"SubExpressionB\":{\"Value\":\"B\",\"Type\":\"Constant\"}" +
                            ",\"Type\":\"Binary\"}", json);
        }

        public interface IExpression2
        {
        }

        public class BinaryExpression2 : IExpression2
        {
            public IExpression2 SubExpressionA { get; set; }
            public IExpression2 SubExpressionB { get; set; }
        }

        public class ManyOrExpression2 : IExpression2
        {
            public List<IExpression2> OrExpr { get; set; }
        }

        public class ConstantExpression2 : IExpression2
        {
            public string Value { get; set; }
        }

        [Test]
        public void TestIfNestedObjectIsDeserialized2()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .Build());

            var binary = JsonSerializer.Deserialize<IExpression2>("{\"Type\":\"Binary\"," +
                                                                 "\"SubExpressionA\":{\"Type\":\"Constant\",\"Value\":\"A\"}," +
                                                                 "\"SubExpressionB\":{\"Type\":\"Constant\",\"Value\":\"B\"}" +
                                                                 "}", options);

            Assert.AreEqual(typeof(ConstantExpression2), (binary as BinaryExpression2)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized2()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .Build());

            var json = JsonSerializer.Serialize<IExpression2>(new BinaryExpression2
            {
                SubExpressionA = new ConstantExpression2 { Value = "A" },
                SubExpressionB = new ConstantExpression2 { Value = "B" }
            }, options);

            Assert.AreEqual("{" +
                            "\"Type\":\"Binary\"," +
                            "\"SubExpressionA\":{\"Type\":\"Constant\",\"Value\":\"A\"}," +
                            "\"SubExpressionB\":{\"Type\":\"Constant\",\"Value\":\"B\"}" +
                            "}", json);
        }

        [Test]
        public void TestNestedObjectInBothWay()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .RegisterSubtype(typeof(ManyOrExpression2), "ManyOr")
                .Build());

            var target = JsonSerializer.Serialize<IExpression2>(new BinaryExpression2
            {
                SubExpressionA = new ManyOrExpression2
                {
                    OrExpr =
                    [
                        new ConstantExpression2 { Value = "A" },
                        new ConstantExpression2 { Value = "B" }
                    ]
                },
                SubExpressionB = new ManyOrExpression2
                {
                    OrExpr =
                    [
                        new ConstantExpression2 { Value = "A" },
                        new ManyOrExpression2
                        {
                            OrExpr =
                            [
                                new ConstantExpression2 { Value = "A" },
                                new ConstantExpression2 { Value = "B" }
                            ]
                        }
                    ]
                }
            }, options);

            var json = "{" +
                       "\"Type\":\"Binary\"," +
                       "\"SubExpressionA\":{\"Type\":\"ManyOr\",\"OrExpr\":[{\"Type\":\"Constant\",\"Value\":\"A\"},{\"Type\":\"Constant\",\"Value\":\"B\"}]}," +
                       "\"SubExpressionB\":{\"Type\":\"ManyOr\",\"OrExpr\":[{\"Type\":\"Constant\",\"Value\":\"A\"},{\"Type\":\"ManyOr\",\"OrExpr\":[{\"Type\":\"Constant\",\"Value\":\"A\"},{\"Type\":\"Constant\",\"Value\":\"B\"}]}]}" +
                       "}";
            Assert.AreEqual(json, target);

            Assert.AreEqual(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<IExpression2>(json, options), options));
        }

        [Test]
        public void TestNestedObjectInBothWayParallel()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IExpression2), "Type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .RegisterSubtype(typeof(ManyOrExpression2), "ManyOr")
                .Build());

            Action test = () =>
            {
                var target = JsonSerializer.Serialize<IExpression2>(new BinaryExpression2
                {
                    SubExpressionA = new ManyOrExpression2
                    {
                        OrExpr =
                        [
                            new ConstantExpression2 { Value = "A" },
                            new ConstantExpression2 { Value = "B" }
                        ]
                    },
                    SubExpressionB = new ManyOrExpression2
                    {
                        OrExpr =
                        [
                            new ConstantExpression2 { Value = "A" },
                            new ManyOrExpression2
                            {
                                OrExpr =
                                [
                                    new ConstantExpression2 { Value = "A" },
                                    new ConstantExpression2 { Value = "B" }
                                ]
                            }
                        ]
                    }
                }, options);

                var json = "{" +
                           "\"Type\":\"Binary\"," +
                           "\"SubExpressionA\":{\"Type\":\"ManyOr\",\"OrExpr\":[{\"Type\":\"Constant\",\"Value\":\"A\"},{\"Type\":\"Constant\",\"Value\":\"B\"}]}," +
                           "\"SubExpressionB\":{\"Type\":\"ManyOr\",\"OrExpr\":[{\"Type\":\"Constant\",\"Value\":\"A\"},{\"Type\":\"ManyOr\",\"OrExpr\":[{\"Type\":\"Constant\",\"Value\":\"A\"},{\"Type\":\"Constant\",\"Value\":\"B\"}]}]}" +
                           "}";
                Assert.AreEqual(json, target);

                Assert.AreEqual(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<IExpression2>(json, options), options));
            };

            Parallel.For(0, 100, index => test());
        }

        [TestFixture]
        public class TwoResolverProfilesOnSameBaseType
        {
            // Two converters registered for the same base type with different discriminator
            // property names and mappings must not interfere through the shared caches. Each
            // options profile resolves its own shape even when used alternately.

            public class Shape
            {
                public int Sides { get; set; }
            }

            public class Circle : Shape
            {
                public double Radius { get; set; }
            }

            public class Square : Shape
            {
                public double Side { get; set; }
            }

            [Test]
            public void ProfilesWithDifferentDiscriminatorNamesDoNotInterfere()
            {
                var kindOptions = new JsonSerializerOptions();
                kindOptions.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Shape>("kind")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("square")
                    .Build());

                var shapeOptions = new JsonSerializerOptions();
                shapeOptions.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Shape>("shape")
                    .RegisterSubtype<Square>("sq")
                    .RegisterSubtype<Circle>("ci")
                    .Build());

                for (int i = 0; i < 3; i++)
                {
                    var circle = JsonSerializer.Deserialize<Shape>("{\"kind\":\"circle\",\"Radius\":2.0}", kindOptions);
                    Assert.IsInstanceOf<Circle>(circle);

                    var square = JsonSerializer.Deserialize<Shape>("{\"shape\":\"sq\",\"Side\":4.0}", shapeOptions);
                    Assert.IsInstanceOf<Square>(square);

                    var squareByKind = JsonSerializer.Deserialize<Shape>("{\"kind\":\"square\",\"Side\":4.0}", kindOptions);
                    Assert.IsInstanceOf<Square>(squareByKind);

                    var circleByShape = JsonSerializer.Deserialize<Shape>("{\"shape\":\"ci\",\"Radius\":2.0}", shapeOptions);
                    Assert.IsInstanceOf<Circle>(circleByShape);
                }
            }

            [Test]
            public void ProfilesWithSameDiscriminatorNameDifferentMappingsDoNotInterfere()
            {
                var optionsA = new JsonSerializerOptions();
                optionsA.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Shape>("type")
                    .RegisterSubtype<Circle>("c")
                    .Build());

                var optionsB = new JsonSerializerOptions();
                optionsB.Converters.Add(JsonSubtypesConverterBuilder
                    .Of<Shape>("type")
                    .RegisterSubtype<Square>("s")
                    .Build());

                for (int i = 0; i < 3; i++)
                {
                    var circle = JsonSerializer.Deserialize<Shape>("{\"type\":\"c\",\"Radius\":2.0}", optionsA);
                    Assert.IsInstanceOf<Circle>(circle);

                    var square = JsonSerializer.Deserialize<Shape>("{\"type\":\"s\",\"Side\":4.0}", optionsB);
                    Assert.IsInstanceOf<Square>(square);
                }
            }
        }
    }
}
