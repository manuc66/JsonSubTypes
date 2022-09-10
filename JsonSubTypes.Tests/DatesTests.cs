using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DatesTest
    {
        [JsonConverter(typeof(JsonSubtypes), nameof(SubTypeClass.Discriminator))]
        [JsonSubtypes.KnownSubType(typeof(SubTypeClass), "SubTypeClass")]
        public abstract class MainClass
        {
        }

        public class SubTypeClass : MainClass
        {
            public string Discriminator => "SubTypeClass";

            public DateTimeOffset? DateTimeOffset { get; set; }
            public DateTime? DateTime { get; set; }
        }

        [Test]
        public void DeserializingSubTypeWithDateParsesCorrectly()
        {
            var json = "{ \"Discriminator\": \"SubTypeClass\", \"DateTime\": \"2020-06-28T00:00:00.00000+00:00\", \"DateTimeOffset\": \"2020-06-28T00:00:00.00000+00:00\" }";

            var obj = JsonConvert.DeserializeObject<MainClass>(json, new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTimeOffset
            });

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj, Is.InstanceOf<SubTypeClass>());
            Assert.That(((SubTypeClass)obj).DateTimeOffset.HasValue, Is.True);
            Assert.That(((SubTypeClass)obj).DateTimeOffset.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(((SubTypeClass)obj).DateTime.HasValue, Is.True);
            Assert.That(((SubTypeClass)obj).DateTime.Value, Is.EqualTo(new DateTime(2020,6,28,0,0,0,0)));
        }
    }
}
