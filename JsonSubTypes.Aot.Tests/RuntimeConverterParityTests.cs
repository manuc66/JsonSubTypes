#nullable enable
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    /// <summary>
    /// Runs the shared <see cref="EngineParityTests"/> scenarios against the runtime converter,
    /// configured through the builders to mirror the shared domain's attributes.
    /// </summary>
    [TestFixture]
    public class RuntimeConverterParityTests : EngineParityTests
    {
        protected override ParityCapabilities Capabilities =>
            ParityCapabilities.ValueDiscriminator | ParityCapabilities.Presence |
            ParityCapabilities.NestedHierarchy | ParityCapabilities.DiscriminatorNameCollision |
            ParityCapabilities.BaseFallbackError;

        protected override JsonSerializerOptions CreateOptions(bool caseInsensitive = false)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = caseInsensitive };
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<Base>("@type")
                .RegisterSubtype<SubB>("SubB")
                .RegisterSubtype<SubC>("SubC")
                .SerializeDiscriminatorProperty()
                .Build());
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<MainClass>(nameof(MainClass.Discriminator))
                .RegisterSubtype<SomeSubtype>("some")
                .SerializeDiscriminatorProperty()
                .Build());
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<IAnimal>("Sound")
                .RegisterSubtype<PDog>("Bark")
                .RegisterSubtype<PCat>("Meow")
                .SerializeDiscriminatorProperty()
                .Build());
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<Payload>("$PayloadKind")
                .RegisterSubtype<Game>(PayloadDiscriminator.GAME)
                .RegisterSubtype<Com>(PayloadDiscriminator.COM)
                .SerializeDiscriminatorProperty()
                .Build());
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<Game>("$GameKind")
                .RegisterSubtype<Run>(GameDiscriminator.RUN)
                .RegisterSubtype<Walk>(GameDiscriminator.WALK)
                .SerializeDiscriminatorProperty()
                .Build());
            options.Converters.Add(JsonSubtypesWithPropertyConverterBuilder.Of<MultiPropBase>()
                .RegisterSubtypeWithProperty<PEmployee>("JobTitle")
                .RegisterSubtypeWithProperty<PEmployee>("Department")
                .RegisterSubtypeWithProperty<PArtist>("Skill")
                .Build());
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<ParameterizedBase>("Kind")
                .RegisterSubtype<ParameterizedDerived>("Derived")
                .SerializeDiscriminatorProperty()
                .Build());
            options.Converters.Add(JsonSubtypesConverterBuilder.Of<DtoBase>("msgType")
                .RegisterSubtype<Foo>(1)
                .SerializeDiscriminatorProperty()
                .Build());
            return options;
        }
    }
}
