using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class ErrorPathTests
    {
        public class RootObject
        {
            public BaseClass Property2 { get; set; }
            public SubClassB Property3 { get; set; }
        }

        public abstract class BaseClass
        {
        }

        public class SubClassA : BaseClass
        {
            public float Value { get; set; }
        }

        public class SubClassB : BaseClass
        {
            public int Value { get; set; }
        }

        private static JsonSerializerSettings CreateSettings(Action<Newtonsoft.Json.Serialization.ErrorEventArgs> onError)
        {
            return new JsonSerializerSettings
            {
                Converters =
                {
                    JsonSubtypesConverterBuilder
                        .Of<BaseClass>("$kind")
                        .RegisterSubtype<SubClassA>("a")
                        .RegisterSubtype<SubClassB>("b")
                        .Build()
                },
                Error = (sender, args) => onError(args)
            };
        }

        [Test]
        public void DeserializationErrorWithinSubtypeReportsFullyQualifiedPath()
        {
            var paths = new List<string>();
            var settings = CreateSettings(args =>
            {
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                {
                    paths.Add(args.ErrorContext.Path);
                }
            });

            JsonReaderException thrown = null;
            try
            {
                using (var jsonReader = new JsonTextReader(new System.IO.StringReader(
                    "{\"Property2\":{\"$kind\":\"a\",\"Value\":\"not a float\"}}")))
                {
                    JsonSerializer.CreateDefault(settings).Deserialize<RootObject>(jsonReader);
                }
            }
            catch (JsonReaderException ex)
            {
                thrown = ex;
            }

            CollectionAssert.Contains(paths, "Property2.Value");
            Assert.That(thrown, Is.Not.Null);
            Assert.AreEqual("Property2.Value", thrown.Path);
        }

        [Test]
        public void DeserializationErrorWithinDirectlyTypedSubtypeReportsFullyQualifiedPath()
        {
            var paths = new List<string>();
            var settings = CreateSettings(args =>
            {
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                {
                    paths.Add(args.ErrorContext.Path);
                }
            });

            JsonReaderException thrown = null;
            try
            {
                using (var jsonReader = new JsonTextReader(new System.IO.StringReader(
                    "{\"Property3\":{\"Value\":\"not an int\"}}")))
                {
                    JsonSerializer.CreateDefault(settings).Deserialize<RootObject>(jsonReader);
                }
            }
            catch (JsonReaderException ex)
            {
                thrown = ex;
            }

            CollectionAssert.Contains(paths, "Property3.Value");
            Assert.That(thrown, Is.Not.Null);
            Assert.AreEqual("Property3.Value", thrown.Path);
        }

        [Test]
        public void DeserializationWithoutErrorsRaisesNoErrorEvent()
        {
            var errors = new List<string>();
            var settings = CreateSettings(args => errors.Add(args.ErrorContext.Path));

            using (var jsonReader = new JsonTextReader(new System.IO.StringReader(
                "{\"Property2\":{\"$kind\":\"a\",\"Value\":3.14},\"Property3\":{\"Value\":42}}")))
            {
                var root = JsonSerializer.CreateDefault(settings).Deserialize<RootObject>(jsonReader);

                Assert.That(root.Property2, Is.InstanceOf<SubClassA>());
                Assert.That(root.Property3, Is.InstanceOf<SubClassB>());
                Assert.That(((SubClassA)root.Property2).Value, Is.EqualTo(3.14f));
            }

            CollectionAssert.IsEmpty(errors);
        }

        public class NestedRootObject
        {
            public BaseClass Property2 { get; set; }
        }

        public class NestedSubClassA : BaseClass
        {
            public BaseClass Nested { get; set; }
        }

        public class NestedSubClassB : BaseClass
        {
            public int Value { get; set; }
        }

        [Test]
        public void DeserializationErrorWithinNestedSubtypeReportsFullyQualifiedPath()
        {
            var paths = new List<string>();
            var settings = new JsonSerializerSettings
            {
                Converters =
                {
                    JsonSubtypesConverterBuilder
                        .Of<BaseClass>("$kind")
                        .RegisterSubtype<NestedSubClassA>("a")
                        .RegisterSubtype<NestedSubClassB>("b")
                        .Build()
                },
                Error = (sender, args) =>
                {
                    if (args.CurrentObject == args.ErrorContext.OriginalObject)
                    {
                        paths.Add(args.ErrorContext.Path);
                    }
                }
            };

            JsonReaderException thrown = null;
            try
            {
                using (var jsonReader = new JsonTextReader(new System.IO.StringReader(
                    "{\"Property2\":{\"$kind\":\"a\",\"Nested\":{\"$kind\":\"b\",\"Value\":\"not an int\"}}}")))
                {
                    JsonSerializer.CreateDefault(settings).Deserialize<NestedRootObject>(jsonReader);
                }
            }
            catch (JsonReaderException ex)
            {
                thrown = ex;
            }

            CollectionAssert.Contains(paths, "Property2.Nested.Value");
            Assert.That(thrown, Is.Not.Null);
            Assert.AreEqual("Property2.Nested.Value", thrown.Path);
        }
    }
}
