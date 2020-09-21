using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DatesTest
    {
        [JsonConverter(typeof(JsonSubtypes), nameof(SubTypeClass.Discriminator))]
        public abstract class MainClass
        {
        }

        public class SubTypeClass : MainClass
        {
            public string Discriminator => "SubTypeClass";

            public DateTimeOffset? Date { get; set; }
        }

        [Test]
        public void DeserializingSubTypeWithDateParsesCorrectly()
        {
            var json = "{ \"Discriminator\": \"MainClass\", \"Date\": \"2020-06-28T00:00:00.00000+00:00\" }";

            var obj = JsonConvert.DeserializeObject<SubTypeClass>(json);

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Date.HasValue, Is.True);
            Assert.That(obj.Date.Value.Offset, Is.EqualTo(TimeSpan.Zero));
        }
    }
}
