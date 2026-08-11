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
        interface IShape<T>
        {
            T Value { get; set; }

            string Kind { get; }
        }

        abstract class ShapeBase<T> : IShape<T>
        {
            public T Value { get; set; }

            public abstract string Kind { get; }
        }

        class Square<T> : ShapeBase<T>
        {
            public override string Kind => "square";
        }

        class Circle<T> : ShapeBase<T>
        {
            public override string Kind => "circle";
        }

        [Test]
        public void Deserialize_BaseConcreteSubtype_WithJsonSubtypes_OnAbstractBase_ReturnsSquare()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(ShapeBase<>), "Kind")
                .RegisterSubtype(typeof(Square<>), "square")
                .RegisterSubtype(typeof(Circle<>), "circle")
                .Build());

            var json = JsonConvert.SerializeObject(new Square<int>
            {
                Value = 42,
            }, settings);

            var shape = JsonConvert.DeserializeObject<ShapeBase<int>>(json, settings);

            Assert.IsInstanceOf<Square<int>>(shape);
            Assert.AreEqual(42, shape.Value);
        }

        [Test]
        public void Deserialize_InterfaceConcreteSubtype_WithJsonSubtypes_OnInterface_ReturnsSquare()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(JsonSubtypesConverterBuilder
                .Of(typeof(IShape<>), "Kind")
                .RegisterSubtype(typeof(Square<>), "square")
                .RegisterSubtype(typeof(Circle<>), "circle")
                .Build());

            var json = JsonConvert.SerializeObject(new Square<int>
            {
                Value = 42,
            }, settings);

            var shape = JsonConvert.DeserializeObject<IShape<int>>(json, settings);

            Assert.IsInstanceOf<Square<int>>(shape);
            Assert.AreEqual(42, shape.Value);
        }
    }

}
