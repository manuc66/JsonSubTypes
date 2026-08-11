using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class MultipleHierarchyLevelsDynamicRegistrationTests
    {
        private JsonSerializerOptions _options;

        [SetUp]
        public void Init()
        {
            _options = new JsonSerializerOptions();
            _options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Payload), Payload.PAYLOAD_KIND)
                .RegisterSubtype(typeof(Game), PayloadDiscriminator.GAME)
                .RegisterSubtype(typeof(Com), PayloadDiscriminator.COM)
                .Build());

            _options.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Game), Game.GAME_KIND)
                .RegisterSubtype(typeof(Walk), GameDiscriminator.WALK)
                .RegisterSubtype(typeof(Run), GameDiscriminator.RUN)
                .Build());
        }

        [Test]
        public void ShouldDeserializeNestedLevel()
        {
            var data = "{\"$GameKind\":0,\"$PayloadKind\":1}";
            Assert.IsInstanceOf<Run>(JsonSerializer.Deserialize<Payload>(data, _options));
        }

        [Test]
        public void ShouldSerializeNestedLevel()
        {
            Payload run = new Run();
            var data = JsonSerializer.Serialize(run, _options);
            Assert.AreEqual("{\"$GameKind\":0,\"$PayloadKind\":1}", data);
        }

        public enum PayloadDiscriminator
        {
            COM = 0,
            GAME = 1
        }

        public enum GameDiscriminator
        {
            RUN = 0,
            WALK = 1
        }

        public abstract class Payload
        {
            public const string PAYLOAD_KIND = "$PayloadKind";

            [JsonPropertyName("$PayloadKind")]
            public PayloadDiscriminator PayloadKind { get; set; } = PayloadDiscriminator.GAME;
        }

        public abstract class Game : Payload
        {
            public const string GAME_KIND = "$GameKind";

            [JsonPropertyName("$GameKind")]
            public GameDiscriminator GameKind { get; set; } = GameDiscriminator.WALK;
        }

        public class Com : Payload
        {
            public Com()
            {
                PayloadKind = PayloadDiscriminator.COM;
            }
        }

        public class Walk : Game
        {
        }

        public class Run : Game
        {
            public Run()
            {
                GameKind = GameDiscriminator.RUN;
            }
        }
    }
}
