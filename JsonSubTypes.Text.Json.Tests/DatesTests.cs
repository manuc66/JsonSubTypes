using System;
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DatesTest
    {
        [JsonSubTypeConverter(typeof(JsonSubtypes<MainClass>), nameof(SubTypeClass.Discriminator))]
        [KnownSubType(typeof(SubTypeClass), "SubTypeClass")]
        public abstract class MainClass
        {
        }

        public class SubTypeClass : MainClass
        {
            public string Discriminator => "SubTypeClass";

            public DateTimeOffset? DateTimeOffset { get; set; }
            public DateTime? DateTime { get; set; }
            public DatesClass DatesClass { get; set; }
        }

        public class NoSubTypeClass
        {
            public DateTimeOffset? DateTimeOffset { get; set; }
            public DateTime? DateTime { get; set; }
            public DatesClass DatesClass { get; set; }
        }

        public class DatesClass
        {
            public DateTimeOffset? DateTimeOffset { get; set; }
            public DateTime? DateTime { get; set; }
        }

        [Test]
        public void DeserializingSubTypeWithNoOffsetDateParsesCorrectly()
        {
            RunDeserializeSubTypeWithOffsetDateTest(new TimeSpan(0, 0, 0));
        }

        [Test]
        public void DeserializingSubTypeWithNegativeOffsetDateParsesCorrectly()
        {
            RunDeserializeSubTypeWithOffsetDateTest(new TimeSpan(-4, 0, 0));
        }

        [Test]
        public void DeserializingSubTypeWithPositiveOffsetDateParsesCorrectly()
        {
            RunDeserializeSubTypeWithOffsetDateTest(new TimeSpan(6, 0, 0));
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserialization()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            var json =
                $"{{ \"Discriminator\": \"SubTypeClass\", \"DateTime\": \"{dateTimeAsString}\", \"DateTimeOffset\": \"{dateTimeAsString}\", \"DatesClass\": {{ \"DateTime\": \"{dateTimeAsString}\", \"DateTimeOffset\": \"{dateTimeAsString}\" }} }}";

            var stock = JsonSerializer.Deserialize<NoSubTypeClass>(json);
            var subtype = JsonSerializer.Deserialize<MainClass>(json);

            Assert.IsInstanceOf<SubTypeClass>(subtype);
            Assert.IsNotNull(stock.DatesClass);
            Assert.IsNotNull(((SubTypeClass)subtype).DatesClass);
            Assert.AreEqual(stock.DateTime.ToString(), ((SubTypeClass)subtype).DateTime.ToString());
            Assert.AreEqual(stock.DateTimeOffset.ToString(), ((SubTypeClass)subtype).DateTimeOffset.ToString());
            Assert.AreEqual(stock.DatesClass.DateTime.ToString(), ((SubTypeClass)subtype).DatesClass.DateTime.ToString());
            Assert.AreEqual(stock.DatesClass.DateTimeOffset.ToString(), ((SubTypeClass)subtype).DatesClass.DateTimeOffset.ToString());
        }

        private void RunDeserializeSubTypeWithOffsetDateTest(TimeSpan offset)
        {
            DateTimeOffset dto = new DateTimeOffset(2020, 06, 28, 0, 0, 0, offset);
            var json = $"{{ \"Discriminator\": \"SubTypeClass\", \"DateTime\": \"{dto:O}\", \"DateTimeOffset\": \"{dto:O}\" }}";

            var obj = JsonSerializer.Deserialize<MainClass>(json);

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj, Is.InstanceOf<SubTypeClass>());
            Assert.That(((SubTypeClass)obj).DateTimeOffset.HasValue, Is.True);
            Assert.That(((SubTypeClass)obj).DateTimeOffset.Value.Offset, Is.EqualTo(offset));
            Assert.That(((SubTypeClass)obj).DateTime.HasValue, Is.True);
        }
    }
}
