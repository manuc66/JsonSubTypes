using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class UnresolvedSubtypeCallbackTests
    {
        public abstract class Animal
        {
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
            string Type { get; set; }
            IList<IStep> Steps { get; set; }
        }

        public class RegisteredStep : IStep
        {
            public string Type { get; set; }
            public IList<IStep> Steps { get; set; }
        }

        public class UnknownStepsTrap : IStep
        {
            public string Type { get; set; }
            public IList<IStep> Steps { get; set; }
        }

        private static JsonSerializerSettings DiscriminatorSettings(IList<UnresolvedSubtypeInfo> notifications,
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

            return new JsonSerializerSettings
            {
                Converters = { builder.Build() }
            };
        }

        [Test]
        public void CallbackNotInvokedWhenSubtypeIsResolved()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var settings = DiscriminatorSettings(notifications, true);

            var result = JsonConvert.DeserializeObject<Animal>("{\"type\":\"Cat\",\"age\":3,\"lives\":7}", settings);

            Assert.IsInstanceOf<Cat>(result);
            Assert.AreEqual(0, notifications.Count);
        }

        [Test]
        public void CallbackInvokedForUnknownDiscriminatorValue()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var settings = DiscriminatorSettings(notifications, true);

            var result = JsonConvert.DeserializeObject<Animal>("{\"type\":\"NonExistentType42\",\"age\":3}", settings);

            Assert.IsInstanceOf<UnknownAnimal>(result);
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
            var settings = DiscriminatorSettings(notifications, true);

            var result = JsonConvert.DeserializeObject<Animal>("{\"age\":3}", settings);

            Assert.IsInstanceOf<UnknownAnimal>(result);
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(typeof(Animal), notifications[0].ParentType);
            Assert.IsNull(notifications[0].DiscriminatorValue);
            Assert.IsFalse(notifications[0].HasDiscriminator);
        }

        [Test]
        public void CallbackInvokedEvenWithoutFallbackSubtype()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var settings = DiscriminatorSettings(notifications, false);

            Assert.Throws<JsonSerializationException>(() =>
                JsonConvert.DeserializeObject<Animal>("{\"type\":\"NonExistentType42\"}", settings));

            Assert.AreEqual(1, notifications.Count);
            Assert.IsNull(notifications[0].FallbackSubtype);
            Assert.AreEqual("NonExistentType42", notifications[0].DiscriminatorValue);
        }

        [Test]
        public void CallbackInvokedForEachUnresolvedElementInTree()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var settings = new JsonSerializerSettings
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

            var result = JsonConvert.DeserializeObject<List<IStep>>(json, settings);

            Assert.IsInstanceOf<UnknownStepsTrap>(result[0]);
            Assert.IsInstanceOf<RegisteredStep>(result[1]);
            Assert.IsInstanceOf<UnknownStepsTrap>(result[2]);
            Assert.IsInstanceOf<UnknownStepsTrap>(result[2].Steps[0]);
            Assert.AreEqual(3, notifications.Count);
            Assert.AreEqual(new[] { "NonExistentType42", "AnotherUnknownType", "AlsoUnknown" },
                notifications.ConvertAll(n => n.DiscriminatorValue));
        }

        [Test]
        public void CallbackInvokedInPropertyPresenceMode()
        {
            var notifications = new List<UnresolvedSubtypeInfo>();
            var settings = new JsonSerializerSettings
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

            var result = JsonConvert.DeserializeObject<Animal>("{\"age\":3}", settings);

            Assert.IsInstanceOf<UnknownAnimal>(result);
            Assert.AreEqual(1, notifications.Count);
            Assert.AreEqual(typeof(Animal), notifications[0].ParentType);
            Assert.IsNull(notifications[0].DiscriminatorPropertyName);
            Assert.IsFalse(notifications[0].HasDiscriminator);
            Assert.AreEqual(typeof(UnknownAnimal), notifications[0].FallbackSubtype);
        }
    }
}
