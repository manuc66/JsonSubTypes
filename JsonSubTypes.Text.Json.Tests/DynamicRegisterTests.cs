using System;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                .RegisterSubtype(typeof(ConstantExpression2), "Constant")
                .RegisterSubtype(typeof(BinaryExpression2), "Binary")
                .Build());

            var json = JsonSerializer.Serialize(new BinaryExpression2
            {
                SubExpressionA = new ConstantExpression2 { Value = "A" },
                SubExpressionB = new ConstantExpression2 { Value = "B" }
            }, options);

            Assert.AreEqual("{" +
                            "\"SubExpressionA\":{\"Value\":\"A\"}," +
                            "\"SubExpressionB\":{\"Value\":\"B\"}" +
                            "}", json);
        }
    }
}
