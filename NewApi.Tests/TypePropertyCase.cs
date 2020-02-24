using System.Text.Json;
using System.Text.Json.Serialization;
using NewApi;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{

    public class TypePropertyCase
    {
        public class TypePropertyCase_LowerWithHigher
        {
            [JsonSubTypeConverter(typeof(JsonSubtypes<DtoBase>), "msgType")]
            [KnownSubType(typeof(Foo), 1)]
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
                var msgType = JsonSerializer.Deserialize<Foo>(serializeObject).MsgType;
                Assert.AreEqual(1, msgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"msgType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }
        }

        public class TypePropertyCase_HigherWithLower
        {
            [JsonSubTypeConverter(typeof(JsonSubtypes<DtoBase>), "MsgType")]
            [KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                [JsonPropertyName("msgType")]
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MsgType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"msgType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }
        }

        public class TypePropertyCase_RedirectLowerWithHigher
        {
            [JsonSubTypeConverter(typeof(JsonSubtypes<DtoBase>), "messageType")]
            [KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                [JsonPropertyName("MessageType")]
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MessageType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"messageType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }
        }

        public class TypePropertyCase_RedirectHigherWithLower
        {
            [JsonSubTypeConverter(typeof(JsonSubtypes<DtoBase>), "MessageType")]
            [KnownSubType(typeof(Foo), 1)]
            public abstract class DtoBase
            {
                [JsonPropertyName("messageType")]
                public virtual int MsgType { get; set; }
            }

            class Foo : DtoBase
            {
            }

            [Test]
            public void FooParsingCamelCase()
            {
                var serializeObject = "{\"MessageType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }

            [Test]
            public void FooParsingLowerPascalCase()
            {
                var serializeObject = "{\"messageType\":1}";
                Assert.AreEqual(1, JsonSerializer.Deserialize<Foo>(serializeObject).MsgType);
                Assert.IsInstanceOf<Foo>(JsonSerializer.Deserialize<DtoBase>(serializeObject));
            }
        }
    }
}
