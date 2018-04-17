using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{

    public class TypePropertyCase
    {
        public class TypePropertyCase_LowerWithHigher
        {
            [JsonConverter(typeof(JsonSubtypes), "msgType")]
            [JsonSubtypes.KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MsgType\":1}";
                var msgType = JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType;
                Assert.AreEqual(1, msgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"msgType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }
        }

        public class TypePropertyCase_HigherWithLower
        {
            [JsonConverter(typeof(JsonSubtypes), "MsgType")]
            [JsonSubtypes.KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                [JsonProperty("msgType")]
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MsgType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"msgType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }
        }

        public class TypePropertyCase_RedirectLowerWithHigher
        {
            [JsonConverter(typeof(JsonSubtypes), "messageType")]
            [JsonSubtypes.KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                [JsonProperty("MessageType")]
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MessageType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"messageType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }
        }

        public class TypePropertyCase_RedirectHigherWithLower
        {
            [JsonConverter(typeof(JsonSubtypes), "MessageType")]
            [JsonSubtypes.KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                [JsonProperty("messageType")]
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MessageType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"messageType\":1}";
                Assert.AreEqual(1, JsonConvert.DeserializeObject<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonConvert.DeserializeObject<DtoBase>(serializeObject));
            }
        }
    }
}