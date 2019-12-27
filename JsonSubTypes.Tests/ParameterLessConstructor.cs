using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [Serializable]
    [JsonConverter(typeof(JsonSubtypes), _type)]
    [JsonSubtypes.KnownSubType(typeof(PlayerAction), EventType.PLAYER_ACTION)]
    [JsonSubtypes.KnownSubType(typeof(GroupAction), EventType.GROUP_ACTION)]
    public abstract class EventClass
    {

        public enum EventType
        {
            PLAYER_ACTION = 0,
            GROUP_ACTION = 1
        }

        private const string _type = "type";

        [JsonProperty(_type)]
        public abstract EventType Discriminator { get; }

    }
    public class GroupAction : EventClass
    {
        public override EventType Discriminator => EventType.GROUP_ACTION;

        [JsonProperty("eventId")]
        public long EventId { get; set; }
        [JsonProperty("groupId")]
        public long GroupId { get; set; }
    }
    public class PlayerAction : EventClass
    {
        public override EventType Discriminator => EventType.PLAYER_ACTION;

        [JsonProperty("eventId")]
        public long EventId { get; set; }
        [JsonProperty("playerId")]
        public long PlayerId { get; set; }

    }
    public class ParameterLessConstructorTests
    {
        [Test]
        public void Test()
        {
            var deserializeObject = JsonConvert.DeserializeObject<EventClass>(@"{
            ""type"":0,
            ""playerId"":33,
            ""eventId"":44
        }");
        }
    }
}
