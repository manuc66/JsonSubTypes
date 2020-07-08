using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    public class DynamicRegisterWithPropertyTests
    {
        public abstract class Animal
        {
            [JsonProperty("age", Order = 1)]
            public int Age { get; set; }
        }

        public class Dog : Animal
        {
            public bool CanBark { get; set; } = true;
        }

        public class Cat : Animal
        {
            [JsonProperty("catLives", Order = 0)]
            public int Lives { get; set; } = 7;
        }

        public abstract class Fish : Animal
        {
            [JsonProperty("fins", Order = 2)]
            public uint FinCount { get; set; }
        }

        public class Shark : Fish
        {
            [JsonProperty("teethRows", Order = 3)]
            public uint TeethRows { get; set; }
        }

        public class HammerheadShark : Shark
        {
            [JsonProperty("hammerSize", Order = 4)]
            public float HammerSize { get; set; }
        }

        [Test]
        public void NestedTypeDeserializeTest()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(Animal))
                .RegisterSubtypeWithProperty(typeof(Cat), "catLives")
                .RegisterSubtypeWithProperty(typeof(Dog), "CanBark")
                // Shark is not registered
                .RegisterSubtypeWithProperty(typeof(HammerheadShark), "hammerSize")
                .Build());

            var json = "{\"age\":11,\"fins\":3,\"teethRows\":4,\"hammerSize\":42.1}";

            var result = JsonConvert.DeserializeObject<Animal>(json);


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
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression))
                .SetFallbackSubtype(typeof(UnknownExpression))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression), "Value")
                .Build());

            var expr = JsonConvert.DeserializeObject<IExpression>("{\"Any\": \"False\"}");

            Assert.AreEqual("False", (expr as UnknownExpression)?.Any);
        }

        [Test]
        public void TestIfNestedObjectIsDeserialized()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression), "SubExpressionB")
                .Build());

            var binary = JsonConvert.DeserializeObject<IExpression>("{" +
                                                                    "\"SubExpressionA\":{\"Value\":\"A\"}," +
                                                                    "\"SubExpressionB\":{\"Value\":\"B\"}" +
                                                                    "}");

            Assert.AreEqual(typeof(ConstantExpression), (binary as BinaryExpression)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression), "SubExpressionB")
                .Build());

            var json = JsonConvert.SerializeObject(new BinaryExpression
            {
                SubExpressionA = new ConstantExpression { Value = "A" },
                SubExpressionB = new ConstantExpression { Value = "B" }
            });

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
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .Build());

            var binary = JsonConvert.DeserializeObject<IExpression2>("{" +
                                                                    "\"SubExpressionA\":{\"Value\":\"A\"}," +
                                                                    "\"SubExpressionB\":{\"Value\":\"B\"}" +
                                                                    "}", settings);
            Assert.AreEqual(typeof(ConstantExpression2), (binary as BinaryExpression2)?.SubExpressionA.GetType());
        }

        [Test]
        public void TestIfNestedObjectIsSerialized2()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .Build());

            var target = JsonConvert.SerializeObject(new BinaryExpression2
            {
                SubExpressionA = new ConstantExpression2 { Value = "A" },
                SubExpressionB = new ConstantExpression2 { Value = "B" }
            });

            Assert.AreEqual("{" +
                            "\"SubExpressionA\":{\"Value\":\"A\"}," +
                            "\"SubExpressionB\":{\"Value\":\"B\"}" +
                            "}", target);
        }

        [Test]
        public void TestNestedObjectInBothWay()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .RegisterSubtypeWithProperty(typeof(ManyOrExpression2), "OrExpr")
                .Build());

            var target = JsonConvert.SerializeObject(new BinaryExpression2
            {
                SubExpressionA = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } },
                SubExpressionB = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } } } }
            });

            var json = "{" +
                           "\"SubExpressionA\":{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}," +
                           "\"SubExpressionB\":{\"OrExpr\":[{\"Value\":\"A\"},{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}]}" +
                           "}";
            Assert.AreEqual(json, target);


            Assert.AreEqual(json, JsonConvert.SerializeObject(JsonConvert.DeserializeObject<IExpression2>(json)));
        }


        [Test]
        public void TestNestedObjectInBothWayParallel()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of(typeof(IExpression2))
                .RegisterSubtypeWithProperty(typeof(ConstantExpression2), "Value")
                .RegisterSubtypeWithProperty(typeof(BinaryExpression2), "SubExpressionB")
                .RegisterSubtypeWithProperty(typeof(ManyOrExpression2), "OrExpr")
                .Build());


            Action test = () =>
            {
                var target = JsonConvert.SerializeObject(new BinaryExpression2
                {
                    SubExpressionA = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } },
                    SubExpressionB = new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ManyOrExpression2 { OrExpr = new List<IExpression2> { new ConstantExpression2 { Value = "A" }, new ConstantExpression2 { Value = "B" } } } } }
                });

                var json = "{" +
                           "\"SubExpressionA\":{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}," +
                           "\"SubExpressionB\":{\"OrExpr\":[{\"Value\":\"A\"},{\"OrExpr\":[{\"Value\":\"A\"},{\"Value\":\"B\"}]}]}" +
                           "}";
                Assert.AreEqual(json, target);


                Assert.AreEqual(json, JsonConvert.SerializeObject(JsonConvert.DeserializeObject<IExpression2>(json)));
            };

            Parallel.For(0, 100, index => test());
        }

        [Test]
        public void RegisterWithGeneric()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
                .Of<Animal>()
                .RegisterSubtypeWithProperty<Cat>("catLives")
                .RegisterSubtypeWithProperty<Dog>("CanBark")
                .Build());

            var json = "{\"catLives\":11,}";

            var result = JsonConvert.DeserializeObject<Animal>(json);

            Assert.AreEqual(typeof(Cat), result.GetType());
        }
    }
}
