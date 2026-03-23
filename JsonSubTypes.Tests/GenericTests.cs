using System;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [JsonConverter(typeof(JsonSubtypes), "Type")]	
    [JsonSubtypes.KnownSubType(typeof(Some<>), "Some")]												
    public interface IResult
    {
        string Type { get;}
    }

    public class SomeInteger: Some<int> {
        public override string ResultType { get { return "SomeInteger"; }} 
		
    }

    public class SomeText: Some<string> {
        public override string ResultType { get { return "SomeText"; }} 
		
    }

    [JsonConverter(typeof(JsonSubtypes), "ResultType")]
    //[JsonSubtypes.KnownSubType(typeof(SomeInteger), "SomeInteger")]
    //[JsonSubtypes.KnownSubType(typeof(SomeText), "SomeText")]
    public abstract class Some<T> : IResult
    {
	
        public string Type { get { return "Some"; }} 
        public abstract string ResultType { get; }
        public T Value { get; set; }
    }
    
    [TestFixture]
    public class GenericTests
    {
        

        [Test]
        public void DeserializingSubTypeWithDateParsesCorrectly()
        {
            var input = new SomeInteger {Value = 42};
            var json = JsonConvert.SerializeObject(input);

            Console.WriteLine(json);
			
            var result = JsonConvert.DeserializeObject<IResult>(json);

            Console.WriteLine(result);
        }
    }
    

    
    [TestFixture]
    public class GenericBaseTests
    {
        interface IBase<T>
        {
            T Value { get; set; }
	
            string Kind { get; }
        }
        abstract class Base<T> : IBase<T> 
        {
            public T Value { get; set; }
	
            public abstract string Kind { get; }
        }

        class Nested1<T> : Base<T>
        {
            public override string Kind => "1";
        }

        class Nested2<T>: Base<T>
        {
            public override string Kind => "2";
        }

        [Test]
        public void Deserialize_BaseConcreteSubtype_WithJsonSubtypes_OnAbstractBase_ReturnsNested1()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(Base<>), "Kind") // type property is only defined here
                .RegisterSubtype(typeof(Nested1<>), "1")
                .RegisterSubtype(typeof(Nested2<>), "2")
                .Build());
	
            var json = JsonConvert.SerializeObject(new Nested1<int>
            {
                Value = 42,
            }, settings); // {"Kind":"1","Value":42}

            var @base = JsonConvert.DeserializeObject<Base<int>>(json, settings); // JsonSerializationException. Could not create an instance of type Base`1[System.Int32]. Type is an interface or abstract class and cannot be instantiated. Path 'Kind', line 1, position 8.
            
            Assert.AreEqual(42, @base.Value);
        }
        
        [Test]
        public void Deserialize_InterfaceConcreteSubtype_WithJsonSubtypes_OnInterface_ReturnsNested1()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IBase<>), "Kind") // type property is only defined here
                .RegisterSubtype(typeof(Nested1<>), "1")
                .RegisterSubtype(typeof(Nested2<>), "2")
                .Build());
	
            var json = JsonConvert.SerializeObject(new Nested1<int>
            {
                Value = 42,
            }, settings); // {"Kind":"1","Value":42}

            var @base = JsonConvert.DeserializeObject<IBase<int>>(json, settings); // JsonSerializationException. Could not create an instance of type Base`1[System.Int32]. Type is an interface or abstract class and cannot be instantiated. Path 'Kind', line 1, position 8.
            
            Assert.AreEqual(42, @base.Value);
        }
    }
    
}
