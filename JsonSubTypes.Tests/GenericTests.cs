using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class GenericTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Type")]
        [JsonSubtypes.KnownSubType(typeof(Some<>), "Some")]
        public interface IResult
        {
            string Type { get; }
        }

        public class SomeInteger : Some<int>
        {
            public override string ResultType { get { return "SomeInteger"; } }
        }

        public class SomeText : Some<string>
        {
            public override string ResultType { get { return "SomeText"; } }
        }

        [JsonConverter(typeof(JsonSubtypes), "ResultType")]
        //[JsonSubtypes.KnownSubType(typeof(SomeInteger), "SomeInteger")]
        //[JsonSubtypes.KnownSubType(typeof(SomeText), "SomeText")]
        public abstract class Some<T> : IResult
        {
            public string Type { get { return "Some"; } }
            public abstract string ResultType { get; }
            public T Value { get; set; }
        }

        [Test]
        public void DeserializingSubTypeWithDateParsesCorrectly()
        {
            var input = new SomeInteger { Value = 42 };
            var json = JsonConvert.SerializeObject(input);

            Console.WriteLine(json);

            var result = JsonConvert.DeserializeObject<IResult>(json);

            Console.WriteLine(result);
        }
    }

    [TestFixture]
    public class KnownBaseType_GenericTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Type")]
        public interface IResult
        {
            string Type { get; }
        }

        [JsonSubtypes.KnownBaseType(typeof(Some<int>), "SomeInteger")]
        public class SomeInteger : Some<int>
        {
            public override string ResultType { get { return "SomeInteger"; } }
        }

        [JsonSubtypes.KnownBaseType(typeof(Some<string>), "SomeText")]
        public class SomeText : Some<string>
        {
            public override string ResultType { get { return "SomeText"; } }
        }

        [JsonConverter(typeof(JsonSubtypes), "ResultType")]
        [JsonSubtypes.KnownBaseType(typeof(IResult), "Some")]
        //[JsonSubtypes.KnownSubType(typeof(SomeInteger), "SomeInteger")]
        //[JsonSubtypes.KnownSubType(typeof(SomeText), "SomeText")]
        public abstract class Some<T> : IResult
        {
            public string Type { get { return "Some"; } }
            public abstract string ResultType { get; }
            public T Value { get; set; }
        }

        [Test]
        public void DeserializingSubTypeWithDateParsesCorrectly()
        {
            var input = new SomeInteger { Value = 42 };
            var json = JsonConvert.SerializeObject(input);

            Console.WriteLine(json);

            var result = JsonConvert.DeserializeObject<IResult>(json);

            Console.WriteLine(result);
        }
    }
}
