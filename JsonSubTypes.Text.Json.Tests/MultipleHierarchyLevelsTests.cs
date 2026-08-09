using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class MultipleHierarchyLevelsTests
    {
        [Test]
        public void ShouldDeserializeNestedLevel()
        {
            var data = "{\"$GameKind\":0,\"$PayloadKind\":1}";
            Assert.IsInstanceOf<Run>(JsonSerializer.Deserialize<Payload>(data));
        }

        [Test]
        public void ShouldSerializeNestedLevel()
        {
            Payload run = new Run();
            var data = JsonSerializer.Serialize(run);
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

        [JsonSubTypeConverter(typeof(JsonSubtypes<Payload>), "$PayloadKind")]
        [KnownSubType(typeof(Game), PayloadDiscriminator.GAME)]
        [KnownSubType(typeof(Com), PayloadDiscriminator.COM)]
        public abstract class Payload
        {
            [JsonPropertyName("$PayloadKind")]
            public PayloadDiscriminator PayloadKind { get; set; } = PayloadDiscriminator.GAME;
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<Game>), "$GameKind")]
        [KnownSubType(typeof(Run), GameDiscriminator.RUN)]
        [KnownSubType(typeof(Walk), GameDiscriminator.WALK)]
        public abstract class Game : Payload
        {
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
