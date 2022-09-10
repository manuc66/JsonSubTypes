using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{

    class SimpleBase
    {
        public String Name { get; set; } 
    }

    class SimpleChildA : SimpleBase
    {
        public int Age { get; set; }
    }

    class SimpleChildB : SimpleBase
    {
        public int Height { get; set; }
    }

    [TestFixture]
    public class DiscriminatorLocationTests
    {
        [Test]
        public void CheckFirst()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(SimpleBase), "type")
                .SerializeDiscriminatorProperty(true)
                .RegisterSubtype(typeof(SimpleChildA), "TypeA")
                .RegisterSubtype(typeof(SimpleChildB), "TypeB")
                .Build());


            SimpleBase test_object = new SimpleChildA() { Name = "bob", Age = 12 };


            var json_first = "{\"type\":\"TypeA\",\"Age\":12,\"Name\":\"bob\"}";

            var result = JsonConvert.SerializeObject(test_object);
            
            Assert.AreEqual(json_first, result);
        }
        
        [Test]
        public void CheckFirstByDefault()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(SimpleBase), "type")
                .SerializeDiscriminatorProperty()
                .RegisterSubtype(typeof(SimpleChildA), "TypeA")
                .RegisterSubtype(typeof(SimpleChildB), "TypeB")
                .Build());


            SimpleBase test_object = new SimpleChildA() { Name = "bob", Age = 12 };


            var json_first = "{\"type\":\"TypeA\",\"Age\":12,\"Name\":\"bob\"}";

            var result = JsonConvert.SerializeObject(test_object);
            
            Assert.AreEqual(json_first, result);
        }

        [Test]
        public void CheckDefaultNotFirst()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(SimpleBase), "type")
                .SerializeDiscriminatorProperty(false)
                .RegisterSubtype(typeof(SimpleChildA), "TypeA")
                .RegisterSubtype(typeof(SimpleChildB), "TypeB")
                .Build());


            SimpleBase test_object = new SimpleChildA() { Name = "bob", Age = 12 };


            var result = JsonConvert.SerializeObject(test_object);
            
            Assert.AreEqual("{\"Age\":12,\"Name\":\"bob\",\"type\":\"TypeA\"}", result);
        }

        [Test]
        public void CheckExplicitLast()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(SimpleBase), "type")
                .SerializeDiscriminatorProperty(false)
                .RegisterSubtype(typeof(SimpleChildA), "TypeA")
                .RegisterSubtype(typeof(SimpleChildB), "TypeB")
                .Build());


            SimpleBase test_object = new SimpleChildA() { Name = "bob", Age = 12 };


            var json_first = "{\"Age\":12,\"Name\":\"bob\",\"type\":\"TypeA\"}";

            var result = JsonConvert.SerializeObject(test_object);

            Assert.AreEqual(json_first, result);
        }

        [Test]
        public void CheckDeserializeFirst()
        {
            var settings = new JsonSerializerSettings();
            JsonConvert.DefaultSettings = () => settings;

            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(SimpleBase), "type")
                .SerializeDiscriminatorProperty(true)
                .RegisterSubtype(typeof(SimpleChildA), "TypeA")
                .RegisterSubtype(typeof(SimpleChildB), "TypeB")
                .Build());


            SimpleBase test_object = new SimpleChildB() { Name = "bob", Height = 36 };

            var json_first = "{\"type\":\"TypeB\",\"Height\":36,\"Name\":\"bob\"}";

            var result = JsonConvert.DeserializeObject<SimpleBase>(json_first);

            Assert.IsInstanceOf(typeof(SimpleChildB), result);

        }
    }
}
