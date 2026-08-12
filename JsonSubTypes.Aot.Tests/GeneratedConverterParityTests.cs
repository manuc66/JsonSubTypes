#nullable enable
using System.Text.Json;
using JsonSubTypes.Aot.Generated;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    /// <summary>
    /// Runs the shared <see cref="EngineParityTests"/> scenarios against the generated converters,
    /// plus the scenarios that only the generated engine supports (nested serialization, and the
    /// attribute-based base-as-leaf write).
    /// </summary>
    [TestFixture]
    public class GeneratedConverterParityTests : EngineParityTests
    {
        protected override JsonSerializerOptions CreateOptions(bool caseInsensitive = false)
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = caseInsensitive,
                Converters =
                {
                    JsonSubTypesAotConverters.Base,
                    JsonSubTypesAotConverters.MainClass,
                    JsonSubTypesAotConverters.IAnimal,
                    JsonSubTypesAotConverters.Payload,
                    JsonSubTypesAotConverters.Game,
                    JsonSubTypesAotConverters.MultiPropBase,
                    JsonSubTypesAotConverters.ParameterizedBase,
                    JsonSubTypesAotConverters.DtoBase
                }
            };
        }

        [Test]
        public void NestedLevelSerialize()
        {
            string data = JsonSerializer.Serialize<Payload>(new Run(), CreateOptions());

            Assert.AreEqual("{\"$PayloadKind\":0,\"$GameKind\":0}", data);
        }

        [Test]
        public void BaseAsLeafWriteRespectsJsonIgnore()
        {
            string json = JsonSerializer.Serialize<PAnimal>(new PAnimal { Name = "Rex" }, CreateOptions());

            Assert.That(json, Does.Not.Contain("Secret"));
        }
    }
}
