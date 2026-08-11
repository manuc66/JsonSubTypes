using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class DynamicRegisterWithPropertyTests
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

        [Test]
        public void NestedTypeDeserializeTest()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(Animal))
                .RegisterSubtypeWithProperty(typeof(Cat), "catLives")
                .RegisterSubtypeWithProperty(typeof(Dog), "CanBark")
                .RegisterSubtypeWithProperty(typeof(HammerheadShark), "hammerSize")
                .Build());

            var json = "{\"age\":11,\"fins\":3,\"teethRows\":4,\"hammerSize\":42.1}";

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
            var jsonSubtypesConverterBuilder = JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(Animal))
                .RegisterSubtypeWithProperty(typeof(Cat), "catLives");

            Assert.Throws<ArgumentException>(() => jsonSubtypesConverterBuilder.RegisterSubtypeWithProperty(typeof(Cat), "catLives"));
        }

        public interface IExpression
        {
        }

        public class BinaryExpression : IExpression
        {
            public IExpression SubExpressionA { get; set; }
            public IExpression SubExpressionB { get; set; }
        }

        public class ConstantExpression : IExpression
        {
            public string Value { get; set; }
        }

        public class NullExpression : IExpression
        {
            public bool Last { get; set; }
        }

        public class UnknownExpression : IExpression
        {
            public string Any { get; set; }
        }

        [Test]
        public void TestFallBack()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression))
                .SetFallbackSubtype(typeof(UnknownExpression))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression), "Value")
                .Build());

            var expr = JsonSerializer.Deserialize<IExpression>("{\"Any\": \"False\"}", options);

            Assert.AreEqual("False", (expr as UnknownExpression)?.Any);
        }

        [Test]
        public void TestIfNestedObjectIsDeserialized()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression), "SubExpressionB")
                .Build());

            var binary = JsonSerializer.Deserialize<IExpression>("{" +
                                                                "\"SubExpressionA\":{\"Value\":\"A\"}," +
                                                                "\"SubExpressionB\":{\"Value\":\"B\"}" +
                                                                "}", options);

            Assert.AreEqual(typeof(ConstantExpression), (binary as BinaryExpression)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression), "SubExpressionB")
                .Build());

            var json = JsonSerializer.Serialize(new BinaryExpression
            {
                SubExpressionA = new ConstantExpression { Value = "A" },
                SubExpressionB = new ConstantExpression { Value = "B" }
            }, options);

            Assert.AreEqual("{" +
                            "\"SubExpressionA\":{\"Value\":\"A\"}," +
                            "\"SubExpressionB\":{\"Value\":\"B\"}" +
                            "}", json);
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
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .Build());

            var binary = JsonSerializer.Deserialize<IExpression2>("{" +
                                                                 "\"SubExpressionA\":{\"Value\":\"A\"}," +
                                                                 "\"SubExpressionB\":{\"Value\":\"B\"}" +
                                                                 "}", options);

            Assert.AreEqual(typeof(ConstantExpression2), (binary as BinaryExpression2)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized2()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
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

        [Test]
        public void TestNestedObjectInBothWay()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .RegisterSubtypeWithProperty(typeof(ManyOrExpression2), "OrExpr")
                .Build());

            var json = "{" +
                       "\"SubExpressionA\":{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}," +
                       "\"SubExpressionB\":{\"OrExpr\":[{\"Value\":\"A\"},{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}]}" +
                       "}";

            var deserialized = JsonSerializer.Deserialize<IExpression2>(json, options);

            Assert.AreEqual(json, JsonSerializer.Serialize<IExpression2>(deserialized, options));
        }

        [Test]
        public void TestNestedObjectInBothWayParallel()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .RegisterSubtypeWithProperty(typeof(ManyOrExpression2), "OrExpr")
                .Build());

            Action test = () =>
            {
                var json = "{" +
                           "\"SubExpressionA\":{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}," +
                           "\"SubExpressionB\":{\"OrExpr\":[{\"Value\":\"A\"},{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}]}" +
                           "}";

                var deserialized = JsonSerializer.Deserialize<IExpression2>(json, options);

                Assert.AreEqual(json, JsonSerializer.Serialize<IExpression2>(deserialized, options));
            };

            Parallel.For(0, 100, index => test());
        }

        [Test]
        public void RegisterWithGeneric()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of<Animal>()
                .RegisterSubtypeWithProperty<Cat>("catLives")
                .RegisterSubtypeWithProperty<Dog>("CanBark")
                .Build());

            var json = "{\"catLives\":11}";

            var result = JsonSerializer.Deserialize<Animal>(json, options);

            Assert.AreEqual(typeof(Cat), result.GetType());
        }
    }
}
