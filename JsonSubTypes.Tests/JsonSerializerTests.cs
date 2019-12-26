using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [JsonConverter(typeof(JsonSubtypes), nameof(Kind))]
    [JsonSubtypes.KnownSubType(typeof(ImplementationA1), "a1")]
    [JsonSubtypes.KnownSubType(typeof(ImplementationA2), "a2")]
    public interface IInterfaceA
    {
        string Kind { get; }
        IInterfaceB SubObject { get; set; }
    }

    public class ImplementationA1 : IInterfaceA
    {
        public string Kind { get; set; } = "a1";
        public IInterfaceB SubObject { get; set; }
    }

    public class ImplementationA2 : IInterfaceA
    {
        public string Kind { get; set; } = "a2";
        public IInterfaceB SubObject { get; set; }
    }

    [JsonConverter(typeof(JsonSubtypes), nameof(Kind))]
    [JsonSubtypes.KnownSubType(typeof(ImplementationB1), "b1")]
    [JsonSubtypes.KnownSubType(typeof(ImplementationB2), "b2")]
    public interface IInterfaceB
    {
        string Kind { get; }
        int Value { get; set; }
    }

    public class ImplementationB1 : IInterfaceB
    {
        public string Kind { get; set; } = "b1";
        public int Value { get; set; }
    }

    public class ImplementationB2 : IInterfaceB
    {
        public string Kind { get; set; } = "b2";
        public int Value { get; set; }
    }
    public class JsonSerializerTests
    {
        [Test]
        public void Test()
        {
            IInterfaceA obj = new ImplementationA1
            {
                SubObject = new ImplementationB1
                {
                    Value = 10,
                },
            };

            var actual = serialize(obj);
            Assert.AreNotEqual("", actual);
        }

        private static string serialize<T>(T obj)
        {
            using (var memoryStream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(memoryStream))
            {
                var serializer = new JsonSerializer();
                //serializer.Converters.Add(
                //    JsonSubtypesConverterBuilder
                //        .Of(typeof(IInterfaceA), nameof(IInterfaceA.Kind))
                //        .RegisterSubtype(typeof(ImplementationA1), "a1")
                //        .RegisterSubtype(typeof(ImplementationA2), "a2")
                //        .SerializeDiscriminatorProperty()
                //        .Build());
                //serializer.Converters.Add(
                //    JsonSubtypesConverterBuilder
                //        .Of(typeof(IInterfaceB), nameof(IInterfaceB.Kind))
                //        .RegisterSubtype(typeof(ImplementationB1), "b1")
                //        .RegisterSubtype(typeof(ImplementationB2), "b2")
                //        .SerializeDiscriminatorProperty()
                //        .Build());
                serializer.Serialize(textWriter, obj);
                textWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
