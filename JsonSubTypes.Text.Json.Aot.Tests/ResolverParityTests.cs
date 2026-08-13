#nullable enable
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Text.Json.Aot.Tests
{
    /// <summary>
    /// Runs the shared <see cref="EngineParityTests"/> scenarios against the native resolver
    /// (<see cref="JsonSubtypesResolver"/>). The resolver only supports the native contract-model
    /// subset, so the scenarios it cannot express are explicitly skipped through
    /// <see cref="EngineParityTests.Capabilities"/>.
    /// </summary>
    [TestFixture]
    public class ResolverParityTests : EngineParityTests
    {
        protected override ParityCapabilities Capabilities => ParityCapabilities.ValueDiscriminator;

        protected override JsonSerializerOptions CreateOptions(bool caseInsensitive = false)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = caseInsensitive };
            options.TypeInfoResolver = JsonSubtypesConverterBuilder.BuildResolvers(
                JsonSubtypesConverterBuilder.Of<IAnimal>("Sound")
                    .RegisterSubtype<PDog>("Bark")
                    .RegisterSubtype<PCat>("Meow")
                    .SerializeDiscriminatorProperty(),
                JsonSubtypesConverterBuilder.Of<DtoBase>("msgType")
                    .RegisterSubtype<Foo>(1)
                    .SerializeDiscriminatorProperty());
            return options;
        }
    }
}
