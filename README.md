# __JsonSubTypes__ a discriminated Json sub-type Converter implementation for .NET
[![Build status](https://ci.appveyor.com/api/projects/status/g11crbl037en6rkq/branch/master?svg=true)](https://ci.appveyor.com/project/manuc66/jsonsubtypes/branch/master)
[![Code Coverage](https://codecov.io/gh/manuc66/JsonSubTypes/branch/master/graph/badge.svg)](https://codecov.io/gh/manuc66/JsonSubTypes)
[![Quality Gate](https://sonarcloud.io/api/badges/gate?key=manuc66:JsonSubtypes:master)](https://sonarcloud.io/dashboard?id=manuc66:JsonSubtypes:master)
[![NuGet](https://img.shields.io/nuget/v/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes/)
[![NuGet](https://img.shields.io/nuget/dt/JsonSubTypes.svg)]()

## DeserializeObject with custom type property name

```csharp
[JsonConverter(typeof(JsonSubtypes), "Kind")]
public interface IAnimal
{
    string Kind { get; }
}

public class Dog : IAnimal
{
    public string Kind { get; } = "Dog";
    public string Breed { get; set; }
}

public class Cat : IAnimal {
    public bool Declawed { get; set;}
}
```

The second parameter of the `JsonConverter` attribute is the JSON property name that will be use to retreive the type information from JSON.

```csharp
var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Kind\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
Assert.AreEqual("Jack Russell Terrier", (Animal as Dog)?.Breed);
```

N.B.: Also works with fully qualified type name

## DeserializeObject with custom type mapping

```csharp
[JsonConverter(typeof(JsonSubtypes), "Sound")]
[JsonSubtypes.KnownSubType(typeof(Dog), "Bark")]
[JsonSubtypes.KnownSubType(typeof(Cat), "Meow")]
public class Animal
{
    public virtual string Sound { get; }
    public string Color { get; set; }
}

public class Dog : Animal
{
    public override string Sound { get; } = "Bark";
    public string Breed { get; set; }
}

public class Cat : Animal
{
    public override string Sound { get; } = "Meow";
    public bool Declawed { get; set; }
}
```

```csharp
var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
Assert.AreEqual("Jack Russell Terrier", (Animal as Dog)?.Breed);
```

N.B.: Also works with other kind of value than string, i.e.: enums, int, ...

## SerializeObject and DeserializeObject with custom type property only present in JSON

This mode of operation only works when JsonSubTypes is explicitely registered in JSON.NET's serializer settings, and not through the ``[JsonConverter]`` attribute. 

```csharp
public abstract class Animal
{
    public int Age { get; set; }
}

public class Dog : Animal
{
    public bool CanBark { get; set; } = true;
}

public class Cat : Animal
{
    public int Lives { get; set; } = 7;
}

public enum AnimalType
{
    Dog = 1,
    Cat = 2
}
```

### Registration:
```csharp
var settings = new JsonSerializerSettings();
settings.Converters.Add(JsonSubtypesConverterBuilder
    .Of(typeof(Animal), "Type") // type property is only defined here
    .RegisterSubtype(typeof(Cat), AnimalType.Cat)
    .RegisterSubtype(typeof(Dog), AnimalType.Dog)
    .SerializeDiscriminatorProperty() // ask to serialize the type property
    .Build());
```

### De-/Serialization:
```csharp
var cat = new Cat { Age = 11, Lives = 6 }

var json = JsonConvert.SerializeObject(cat, settings);

Assert.Equal("{\"Lives\":6,\"Age\":11,\"Type\":2}", json);

var result = JsonConvert.DeserializeObject<Animal>(json, settings);

Assert.Equal(typeof(Cat), result.GetType());
Assert.Equal(11, result.Age);
Assert.Equal(6, (result as Cat)?.Lives);
```

## DeserializeObject mapping by property presence

```csharp
[JsonConverter(typeof(JsonSubtypes))]
[JsonSubtypes.KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
[JsonSubtypes.KnownSubTypeWithProperty(typeof(Artist), "Skill")]
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class Employee : Person
{
    public string Department { get; set; }
    public string JobTitle { get; set; }
}

public class Artist : Person
{
    public string Skill { get; set; }
}
```


```csharp
string json = "[{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                "{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                "{\"Skill\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";


var persons = JsonConvert.DeserializeObject<IReadOnlyCollection<Person>>(json);
Assert.AreEqual("Painter", (persons.Last() as Artist)?.Skill);
```

## 💖 Support this project
If this project helped you save money or time or simply makes your life also easier, you can give me a cup of coffee =)

- [![Support via PayPal](https://cdn.rawgit.com/twolfson/paypal-github-button/1.0.0/dist/button.svg)](https://www.paypal.me/manuc66)
- Bitcoin — You can send me bitcoins at this address: `33gxVjey6g4Beha26fSQZLFfWWndT1oY3F`