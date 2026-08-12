using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class GenericEdgeCaseTests
    {
        public abstract class ArityBase<T>
        {
            public abstract string Kind { get; }
        }

        public class TwoParam<T, U> : ArityBase<T>
        {
            public override string Kind => "1";
        }

        [Test]
        public void ArityMismatchThrowsCleanJsonSerializationException()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(ArityBase<>), "Kind")
                .RegisterSubtype(typeof(TwoParam<,>), "1")
                .Build());

            var json = "{\"Kind\":\"1\"}";

            var exception = Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<ArityBase<int>>(json, settings));
            Assert.That(exception.Message, Does.Contain("generic").IgnoreCase);
        }

        public interface IShape<T>
        {
            T Value { get; set; }
        }

        public abstract class ShapeBase<T> : IShape<T>
        {
            public T Value { get; set; }
            public abstract string Kind { get; }
        }

        public class Circle<T> : ShapeBase<T>
        {
            public override string Kind => "circle";
        }

        public class UnrelatedShape<T> : IShape<T>
        {
            public T Value { get; set; }
        }

        [Test]
        public void UnrelatedInterfaceImplementorIsNotClaimed()
        {
            var converter = JsonSubtypesConverterBuilder
                .Of(typeof(IShape<>), "Kind")
                .RegisterSubtype(typeof(Circle<>), "circle")
                .Build();

            Assert.IsFalse(converter.CanConvert(typeof(UnrelatedShape<int>)));
        }

        [Test]
        public void UnrelatedInterfaceImplementorSerializesNormallyWithDiscriminator()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IShape<>), "Kind")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Circle<>), "circle")
                .Build());

            var json = JsonConvert.SerializeObject(new UnrelatedShape<int> { Value = 5 }, settings);

            Assert.AreEqual("{\"Value\":5}", json);
        }

        public abstract class MultiBase<T>
        {
            public abstract string Kind { get; }
        }

        public abstract class MultiMid<T> : MultiBase<T>
        {
        }

        public class MultiLeaf<T> : MultiMid<T>
        {
            public override string Kind => "leaf";
        }

        [Test]
        public void MultiLevelGenericHierarchyDeserializes()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(MultiBase<>), "Kind")
                .RegisterSubtype(typeof(MultiLeaf<>), "leaf")
                .Build());

            var result = JsonConvert.DeserializeObject<MultiBase<int>>("{\"Kind\":\"leaf\"}", settings);

            Assert.IsInstanceOf<MultiLeaf<int>>(result);
        }

        public class BareBase<T>
        {
        }

        public class BareOne<T> : BareBase<T>
        {
        }

        [Test]
        public void ExplicitClosedFormRegistrationTakesPrecedence()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(BareBase<>), "Kind")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(BareOne<>), "1")
                .RegisterSubtype(typeof(BareOne<int>), "5")
                .Build());

            var jsonInt = JsonConvert.SerializeObject(new BareOne<int>(), settings);
            StringAssert.Contains("\"Kind\":\"5\"", jsonInt);

            var jsonString = JsonConvert.SerializeObject(new BareOne<string>(), settings);
            StringAssert.Contains("\"Kind\":\"1\"", jsonString);
        }

        public class GenericFallback<T> : BareBase<T>
        {
        }

        [Test]
        public void GenericFallbackSubtypeIsClosed()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(BareBase<>), "Kind")
                .RegisterSubtype(typeof(BareOne<>), "1")
                .SetFallbackSubtype(typeof(GenericFallback<>))
                .Build());

            var result = JsonConvert.DeserializeObject<BareBase<int>>("{\"Kind\":\"zzz\"}", settings);

            Assert.IsInstanceOf<GenericFallback<int>>(result);
        }
    }
}
