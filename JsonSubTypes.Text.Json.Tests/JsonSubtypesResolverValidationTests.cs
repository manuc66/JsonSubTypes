using System;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class JsonSubtypesResolverValidationTests
    {
        public enum Color
        {
            Red,
            Green
        }

        private static JsonSubtypesConverterBuilder SimpleBuilder()
        {
            return JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>("circle")
                .RegisterSubtype<Square>("square")
                .SerializeDiscriminatorProperty();
        }

        [Test]
        public void NoRegisteredSubtype_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildResolver());

            Assert.AreEqual(
                "Cannot build a type info resolver without any registered subtype. Call RegisterSubtype before building.",
                exception?.Message);
        }

        [Test]
        public void FallbackSubtype_Throws()
        {
            var builder = SimpleBuilder().SetFallbackSubtype<Square>();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("SetFallbackSubtype is not supported", exception?.Message);
        }

        [Test]
        public void ReadOnlyModeWithoutSerializeDiscriminator_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>("circle")
                .RegisterSubtype<Square>("square");

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("always serializes the discriminator property", exception?.Message);
        }

        [Test]
        public void SerializeDiscriminatorLast_Throws()
        {
            var builder = SimpleBuilder().SerializeDiscriminatorProperty(addDiscriminatorFirst: false);

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("always writes the discriminator property first", exception?.Message);
        }

        [Test]
        public void NullDiscriminator_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>(null)
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("null discriminator value is not supported", exception?.Message);
        }

        [Test]
        public void EnumDiscriminator_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>(Color.Red)
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("only string and int are supported", exception?.Message);
        }

        [Test]
        public void LongDiscriminator_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>(42L)
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("only string and int are supported", exception?.Message);
        }

        [Test]
        public void GuidDiscriminator_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>(Guid.NewGuid())
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<NotSupportedException>(() => builder.BuildResolver());

            StringAssert.Contains("only string and int are supported", exception?.Message);
        }

        [Test]
        public void MultipleDiscriminatorsOnSameType_Throws()
        {
            var builder = JsonSubtypesConverterBuilder.Of<IShape>("$type")
                .RegisterSubtype<Circle>("circle")
                .RegisterSubtype<Circle>("round")
                .SerializeDiscriminatorProperty();

            var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildResolver());

            StringAssert.Contains("Multiple discriminators on single type are not supported", exception?.Message);
        }

        [Test]
        public void DuplicateDiscriminatorValue_ThrowsAtRegistration()
        {
            var exception = Assert.Throws<ArgumentException>(() =>
                JsonSubtypesConverterBuilder.Of<IShape>("$type")
                    .RegisterSubtype<Circle>("circle")
                    .RegisterSubtype<Square>("circle"));

            Assert.IsNotNull(exception);
        }

        [Test]
        public void BuildResolver_DoesNotMutateBuilder_CanStillBuildConverter()
        {
            var builder = SimpleBuilder();

            builder.BuildResolver();

            var converter = builder.Build();
            Assert.IsNotNull(converter);
        }
    }
}
