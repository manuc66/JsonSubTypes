using Newtonsoft.Json;
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
            Assert.IsInstanceOf<Run>(JsonConvert.DeserializeObject<Payload>(data));
        }

        [Test]
        public void ShouldSerializeNestedLevel()
        {
            Payload run = new Run();
            var data = JsonConvert.SerializeObject(run);
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

        [JsonConverter(typeof(JsonSubtypes), PAYLOAD_KIND)]
        [JsonSubtypes.KnownSubType(typeof(Game), PayloadDiscriminator.GAME)]
        [JsonSubtypes.KnownSubType(typeof(Com), PayloadDiscriminator.COM)]
        public abstract class Payload
        {
            public const string PAYLOAD_KIND = "$PayloadKind";

            [JsonProperty(PAYLOAD_KIND)]
            protected abstract PayloadDiscriminator PayloadKind { get; }
        }

        [JsonConverter(typeof(JsonSubtypes), GAME_KIND)]
        [JsonSubtypes.KnownSubType(typeof(Run), GameDiscriminator.RUN)]
        [JsonSubtypes.KnownSubType(typeof(Walk), GameDiscriminator.WALK)]
        public abstract class Game : Payload
        {
            protected override PayloadDiscriminator PayloadKind => PayloadDiscriminator.GAME;

            public const string GAME_KIND = "$GameKind";

            [JsonProperty(GAME_KIND)]
            protected abstract GameDiscriminator GameKind { get; }
        }

        public class Com : Payload
        {
            protected override PayloadDiscriminator PayloadKind => PayloadDiscriminator.COM;
        }

        public class Walk : Game
        {
            protected override GameDiscriminator GameKind => GameDiscriminator.WALK;
        }

        public class Run : Game
        {
            protected override GameDiscriminator GameKind => GameDiscriminator.RUN;
        }
    }

    [TestFixture]
    public class MultipleHierarchyLevelsDynamicRegistrationTests
    {
        JsonSerializerSettings settings;
        [SetUp]
        public void Init()
        {
            settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Payload), Payload.PAYLOAD_KIND)
                .RegisterSubtype(typeof(Game), PayloadDiscriminator.GAME)
                .RegisterSubtype(typeof(Com), PayloadDiscriminator.COM)
                .Build());

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Game), Game.GAME_KIND)
                .RegisterSubtype(typeof(Walk), GameDiscriminator.WALK)
                .RegisterSubtype(typeof(Run), GameDiscriminator.RUN)
                .Build());
        }

        [Test]
        public void ShouldDeserializeNestedLevel()
        {
            var data = "{\"$GameKind\":0,\"$PayloadKind\":1}";
            Assert.IsInstanceOf<Run>(JsonConvert.DeserializeObject<Payload>(data, settings));
        }

        [Test]
        public void ShouldSerializeNestedLevel()
        {
            Payload run = new Run();
            var data = JsonConvert.SerializeObject(run, settings);
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

            [JsonProperty(PAYLOAD_KIND)]
            protected abstract PayloadDiscriminator PayloadKind { get; }
        }

        public abstract class Game : Payload
        {
            protected override PayloadDiscriminator PayloadKind => PayloadDiscriminator.GAME;

            public const string GAME_KIND = "$GameKind";

            [JsonProperty(GAME_KIND)]
            protected abstract GameDiscriminator GameKind { get; }
        }

        public class Com : Payload
        {
            protected override PayloadDiscriminator PayloadKind => PayloadDiscriminator.COM;
        }

        public class Walk : Game
        {
            protected override GameDiscriminator GameKind => GameDiscriminator.WALK;
        }

        public class Run : Game
        {
            protected override GameDiscriminator GameKind => GameDiscriminator.RUN;
        }
    }

    [TestFixture]
    public class MultipleHierarchyLevelsDynamicRegistrationWriteDiscriminatorTests
    {
        JsonSerializerSettings settings;
        [SetUp]
        public void Init()
        {
            settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Payload), Payload.PAYLOAD_KIND)
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Game), PayloadDiscriminator.GAME)
                .RegisterSubtype(typeof(Com), PayloadDiscriminator.COM)
                .Build());

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Game), Game.GAME_KIND)
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(Walk), GameDiscriminator.WALK)
                .RegisterSubtype(typeof(Run), GameDiscriminator.RUN)
                .Build());
        }

        [Test]
        public void ShouldDeserializeNestedLevel()
        {
            var data = "{\"$GameKind\":0,\"$PayloadKind\":1}";
            Assert.IsInstanceOf<Run>(JsonConvert.DeserializeObject<Payload>(data, settings));
        }

        [Test]
        [Ignore("Not supported: not implemented")]
        public void ShouldSerializeNestedLevel()
        {
            Payload run = new Run();
            var data = JsonConvert.SerializeObject(run, settings);
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
        }

        public abstract class Game : Payload
        {
            public const string GAME_KIND = "$GameKind";

        }

        public class Com : Payload
        {
        }

        public class Walk : Game
        {
        }

        public class Run : Game
        {
        }
    }
}
