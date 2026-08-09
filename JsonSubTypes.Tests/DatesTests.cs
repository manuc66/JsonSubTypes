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
        public void DeserializingSubTypeMatchesStockDeserializationWithSerializerSettingDefaults()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, settings.DateParseHandling, settings.DateTimeZoneHandling);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeAndDateTimeZoneHandlingUtc()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTime, DateTimeZoneHandling.Utc);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeAndDateTimeZoneHandlingLocal()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTime, DateTimeZoneHandling.Local);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeAndDateTimeZoneHandlingRoundtripKind()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTime, DateTimeZoneHandling.RoundtripKind);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeAndDateTimeZoneHandlingUnspecified()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTime, DateTimeZoneHandling.Unspecified);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeOffsetAndDateTimeZoneHandlingUtc()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTimeOffset, DateTimeZoneHandling.Utc);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeOffsetAndDateTimeZoneHandlingLocal()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTimeOffset, DateTimeZoneHandling.Local);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeOffsetAndDateTimeZoneHandlingRoundtripKind()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTimeOffset, DateTimeZoneHandling.RoundtripKind);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingDateTimeOffsetAndDateTimeZoneHandlingUnspecified()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.DateTimeOffset, DateTimeZoneHandling.Unspecified);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingNoneAndDateTimeZoneHandlingUtc()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.None, DateTimeZoneHandling.Utc);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingNoneAndDateTimeZoneHandlingLocal()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.None, DateTimeZoneHandling.Local);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingNoneAndDateTimeZoneHandlingRoundtripKind()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.None, DateTimeZoneHandling.RoundtripKind);
        }

        [Test]
        public void DeserializingSubTypeMatchesStockDeserializationWithDateParseHandlingNoneAndDateTimeZoneHandlingUnspecified()
        {
            string dateTimeAsString = "2020-06-28T00:00:00.00000-04:00";
            JsonSerializerSettings settings = new JsonSerializerSettings();
            RunSubtypeMatchesStockDDeserializationTest(dateTimeAsString, DateParseHandling.None, DateTimeZoneHandling.Unspecified);
        }

        private void RunSubtypeMatchesStockDDeserializationTest(string dateTime, DateParseHandling dateParseHandling, DateTimeZoneHandling dateTimeZoneHandling)
        {
            var json = $" {{ \"Discriminator\": \"SubTypeClass\", \"DateTime\": \"{dateTime}\", \"DateTimeOffset\": \"{dateTime}\", \"DatesClass\": {{ \"DateTime\": \"{dateTime}\", \"DateTimeOffset\": \"{dateTime}\" }} }}";
            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                DateParseHandling = dateParseHandling,
                DateTimeZoneHandling = dateTimeZoneHandling
            };

            // stock Newtonsoft Deserialization
            var stock = JsonConvert.DeserializeObject<NoSubTypeClass>(json, jsonSerializerSettings);
            // JsonSubtypes Deserialization
            var subtype = JsonConvert.DeserializeObject<MainClass>(json, jsonSerializerSettings);

            Assert.IsNotNull(stock);
            Assert.IsNotNull(subtype);
            Assert.IsInstanceOf(typeof(NoSubTypeClass), stock);
            Assert.IsInstanceOf(typeof(SubTypeClass), subtype);
            Assert.IsNotNull(stock.DatesClass);
            Assert.IsNotNull(((SubTypeClass)subtype).DatesClass);
            Assert.IsInstanceOf(typeof(DatesClass), stock.DatesClass);
            Assert.IsInstanceOf(typeof(DatesClass), ((SubTypeClass)subtype).DatesClass);
            // Compare ToString values as AreEqual on DateTimeOffset will use offsets when determining equivalence
            Assert.AreEqual(stock.DateTime.ToString(), ((SubTypeClass)subtype).DateTime.ToString(), $"DateTime With {dateParseHandling} & {dateTimeZoneHandling}");
            Assert.AreEqual(stock.DateTimeOffset.ToString(), ((SubTypeClass)subtype).DateTimeOffset.ToString(), $"DateTimeOffset With {dateParseHandling} & {dateTimeZoneHandling}");
            Assert.AreEqual(stock.DatesClass.DateTime.ToString(), ((SubTypeClass)subtype).DatesClass.DateTime.ToString(), $"DatesClass.DateTime With {dateParseHandling} & {dateTimeZoneHandling}");
            Assert.AreEqual(stock.DatesClass.DateTimeOffset.ToString(), ((SubTypeClass)subtype).DatesClass.DateTimeOffset.ToString(), $"DatesClass.DateTime With {dateParseHandling} & {dateTimeZoneHandling}");
        }

        private void RunDeserializeSubTypeWithOffsetDateTest(TimeSpan offset)
        {
            DateTimeOffset dto = new DateTimeOffset(2020, 06, 28, 0, 0, 0, offset);
            DateTimeOffset local = dto.ToLocalTime();
            var json = $"{{ \"Discriminator\": \"SubTypeClass\", \"DateTime\": \"{dto:O}\", \"DateTimeOffset\": \"{dto:O}\" }}";

            var obj = JsonConvert.DeserializeObject<MainClass>(json);

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj, Is.InstanceOf<SubTypeClass>());
            Assert.That(((SubTypeClass)obj).DateTimeOffset.HasValue, Is.True);
            Assert.That(((SubTypeClass)obj).DateTimeOffset.Value.Offset, Is.EqualTo(offset));
            Assert.That(((SubTypeClass)obj).DateTime.HasValue, Is.True);
            Assert.That(((SubTypeClass)obj).DateTime.Value.ToString(), Is.EqualTo(local.DateTime.ToString()));
        }
    }
}
