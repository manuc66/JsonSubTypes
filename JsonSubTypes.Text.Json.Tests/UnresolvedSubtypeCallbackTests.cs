using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class UnresolvedSubtypeCallbackTests
    {
        public abstract class Animal
        {
            [JsonPropertyName("age")]
            public int Age { get; set; }
        }

        public class Dog : Animal
        {
            public bool CanBark { get; set; }
        }

        public class Cat : Animal
        {
            public int Lives { get; set; }
        }

        public class UnknownAnimal : Animal
        {
        }

        public interface IStep
        {
            [JsonPropertyName("Type")]
            string Type { get; set; }

            List<IStep> Steps { get; set; }
        }

        public class RegisteredStep : IStep
        {
            [JsonPropertyName("Type")]
            public string Type { get; set; }

            public List<IStep> Steps { get; set; }
        }

        public class UnknownStepsTrap : IStep
        {
            [JsonPropertyName("Type")]
            public string Type { get; set; }

            public List<IStep> Steps { get; set; }
        }

        private static JsonSerializerOptions DiscriminatorOptions(IList<UnresolvedSubtypeInfo> notifications,
            bool withFallback)
        {
            var builder = JsonSubtypesConverterBuilder
                .Of(typeof(Animal), "type")
                .RegisterSubtype(typeof(Cat), "Cat")
                .RegisterSubtype(typeof(Dog), "Dog")
                .OnUnresolvedSubtype(notifications.Add);
            if (withFallback)
            {
                builder.SetFallbackSubtype(typeof(UnknownAnimal));
            }

            return new JsonSerializerOptions
            {
                Converters = { builder.Build() }
            };
        }

        [Test]
        public void CallbackNotInvokedWhenSubtypeIsResolved()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var options = DiscriminatorOptions(notifications, true);

            var result = JsonSerializer.Deserialize<Animal>("{\"type\":\"Cat\",\"age\":3,\"lives\":7}", options);

            Assert.AreEqual(typeof(Cat), result?.GetType());
            Assert.AreEqual(0, notifications.Count);
        }

        [Test]
        public void CallbackInvokedForUnknownDiscriminatorValue()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var options = DiscriminatorOptions(notifications, true);

            var result = JsonSerializer.Deserialize<Animal>("{\"type\":\"NonExistentType42\",\"age\":3}", options);

            Assert.AreEqual(typeof(UnknownAnimal), result?.GetType());
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(typeof(Animal), notifications[0].ParentType);
            Assert.AreEqual("type", notifications[0].DiscriminatorPropertyName);
            Assert.AreEqual("NonExistentType42", notifications[0].DiscriminatorValue);
            Assert.IsTrue(notifications[0].HasDiscriminator);
            Assert.AreEqual(typeof(UnknownAnimal), notifications[0].FallbackSubtype);
        }

        [Test]
        public void CallbackInvokedForMissingDiscriminator()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var options = DiscriminatorOptions(notifications, true);

            var result = JsonSerializer.Deserialize<Animal>("{\"age\":3}", options);

            Assert.AreEqual(typeof(UnknownAnimal), result?.GetType());
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(typeof(Animal), notifications[0].ParentType);
            Assert.IsNull(notifications[0].DiscriminatorValue);
            Assert.IsFalse(notifications[0].HasDiscriminator);
        }

        [Test]
        public void CallbackInvokedEvenWithoutFallbackSubtype()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var options = DiscriminatorOptions(notifications, false);

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<Animal>("{\"type\":\"NonExistentType42\"}", options));

            Assert.AreEqual(1, notifications.Count);
            Assert.IsNull(notifications[0].FallbackSubtype);
            Assert.AreEqual("NonExistentType42", notifications[0].DiscriminatorValue);
        }

        [Test]
        public void CallbackInvokedForEachUnresolvedElementInTree()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    JsonSubtypesConverterBuilder
                        .Of(typeof(IStep), "Type")
                        .SetFallbackSubtype(typeof(UnknownStepsTrap))
                        .RegisterSubtype(typeof(RegisteredStep), "RegisteredStep")
                        .OnUnresolvedSubtype(notifications.Add)
                        .Build()
                }
            };

            var json = "[" +
                       "{\"Type\":\"NonExistentType42\"}," +
                       "{\"Type\":\"RegisteredStep\"}," +
                       "{\"Type\":\"AnotherUnknownType\",\"Steps\":[{\"Type\":\"AlsoUnknown\"}]}" +
                       "]";

            var result = JsonSerializer.Deserialize<List<IStep>>(json, options);

            Assert.AreEqual(3, result.Count);
            Assert.IsInstanceOf<UnknownStepsTrap>(result[0]);
            Assert.IsInstanceOf<RegisteredStep>(result[1]);
            Assert.IsInstanceOf<UnknownStepsTrap>(result[2]);
            Assert.IsInstanceOf<UnknownStepsTrap>(result[2].Steps[0]);
            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual("NonExistentType42", notifications[0].DiscriminatorValue);
            Assert.AreEqual("AnotherUnknownType", notifications[1].DiscriminatorValue);
            Assert.AreEqual("AlsoUnknown", notifications[2].DiscriminatorValue);
        }

        [Test]
        public void CallbackInvokedInPropertyPresenceMode()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    JsonSubtypesWithPropertyConverterBuilder
                        .Of(typeof(Animal))
                        .RegisterSubtypeWithProperty(typeof(Cat), "catLives")
                        .RegisterSubtypeWithProperty(typeof(Dog), "canBark")
                        .SetFallbackSubtype(typeof(UnknownAnimal))
                        .OnUnresolvedSubtype(notifications.Add)
                        .Build()
                }
            };

            var result = JsonSerializer.Deserialize<Animal>("{\"age\":3}", options);

            Assert.AreEqual(typeof(UnknownAnimal), result?.GetType());
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(typeof(Animal), notifications[0].ParentType);
            Assert.IsNull(notifications[0].DiscriminatorPropertyName);
            Assert.IsFalse(notifications[0].HasDiscriminator);
            Assert.AreEqual(typeof(UnknownAnimal), notifications[0].FallbackSubtype);
        }
    }
}
