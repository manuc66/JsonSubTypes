# __JsonSubTypes__
__JsonSubTypes__ is a discriminated Json sub-type Converter implementation for .NET

[![Build status](https://ci.appveyor.com/api/projects/status/g11crbl037en6rkq/branch/master?svg=true)](https://ci.appveyor.com/project/manuc66/jsonsubtypes/branch/master)
[![Code Coverage](https://codecov.io/gh/manuc66/JsonSubTypes/branch/master/graph/badge.svg)](https://codecov.io/gh/manuc66/JsonSubTypes)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=manuc66%3AJsonSubtypes&metric=alert_status)](https://sonarcloud.io/dashboard?id=manuc66%3AJsonSubtypes)
[![NuGet](https://img.shields.io/nuget/v/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes/)
[![NuGet](https://img.shields.io/nuget/dt/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes)
[![CodeFactor](https://www.codefactor.io/repository/github/manuc66/JsonSubTypes/badge)](https://www.codefactor.io/repository/github/manuc66/JsonSubTypes)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes?ref=badge_shield)

## System.Text.Json support (JsonSubTypes.Text.Json)

A variant of the library for `System.Text.Json` (.NET 8+) is available in the `JsonSubTypes.Text.Json` namespace and package. It supports the same attribute-driven and builder-driven API, adapted to `System.Text.Json` idioms.

### Attribute based discriminator

```csharp
using JsonSubTypes.Text.Json;

[JsonSubTypeConverter(typeof(JsonSubtypes<Animal>), "Sound")]
[KnownSubType(typeof(Dog), "Bark")]
[KnownSubType(typeof(Cat), "Meow")]
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
var animal = JsonSerializer.Deserialize<Animal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
```

### Builder based dynamic registration

```csharp
var options = new JsonSerializerOptions();
options.Converters.Add(JsonSubtypesConverterBuilder
    .Of(typeof(Animal), "type")
    .RegisterSubtype(typeof(Cat), AnimalType.Cat)
    .RegisterSubtype(typeof(Dog), AnimalType.Dog)
    .Build());

var result = JsonSerializer.Deserialize<Animal>("{\"catLives\":6,\"type\":2,\"age\":11}", options);
Assert.AreEqual(typeof(Cat), result.GetType());
```

### Mapping by property presence

```csharp
[JsonSubTypeConverter(typeof(JsonSubtypes<Person>))]
[KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
[KnownSubTypeWithProperty(typeof(Artist), "Skill")]
public class Person { }
```

### Fallback subtype

```csharp
[JsonSubTypeConverter(typeof(JsonSubtypes<IExpression>), "Type")]
[KnownSubType(typeof(ConstantExpression), "Constant")]
[FallBackSubType(typeof(UnknownExpression))]
public interface IExpression { }
```

### Differences with the Newtonsoft.Json version

- The discriminator property is **not** written during serialization: serialization delegates to `System.Text.Json` using the runtime type. `SerializeDiscriminatorProperty()` is therefore not provided.
- `JsonNamingPolicy` and `PropertyNameCaseInsensitive` are respected when matching the discriminator property, and `JsonStringEnumConverter` is respected when mapping discriminator values.
- Dotted or nested discriminator property paths (e.g. `"nested.property"`) are supported.

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
    public string Kind { get; } = "Cat";
    public bool Declawed { get; set;}
}
```

The second parameter of the `JsonConverter` attribute is the JSON property name that will be use to retreive the type information from JSON.

```csharp
var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Kind\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
```

N.B.: This only works for types in the same assembly as the base type/interface and either in the same namespace or with a fully qualified type name.

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
Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
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

or using syntax with generics:

```csharp
var settings = new JsonSerializerSettings();
settings.Converters.Add(JsonSubtypesConverterBuilder
    .Of<Animal>("Type") // type property is only defined here
    .RegisterSubtype<Cat>(AnimalType.Cat)
    .RegisterSubtype<Dog>(AnimalType.Dog)
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

or using syntax with generics:


```csharp
string json = "[{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                "{\"Department\":\"Department1\",\"JobTitle\":\"JobTitle1\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}," +
                "{\"Skill\":\"Painter\",\"FirstName\":\"FirstName1\",\"LastName\":\"LastName1\"}]";


var persons = JsonConvert.DeserializeObject<IReadOnlyCollection<Person>>(json);
Assert.AreEqual("Painter", (persons.Last() as Artist)?.Skill);
```


### Registration:
```cs
settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
    .Of(typeof(Person))
    .RegisterSubtypeWithProperty(typeof(Employee), "JobTitle")
    .RegisterSubtypeWithProperty(typeof(Artist), "Skill")
    .Build());
```

or

```cs
settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
    .Of<Person>()
    .RegisterSubtypeWithProperty<Employee>("JobTitle")
    .RegisterSubtypeWithProperty<Artist>("Skill")
    .Build());
```


## A default class other than the base type can be defined

```cs
[JsonConverter(typeof(JsonSubtypes))]
[JsonSubtypes.KnownSubType(typeof(ConstantExpression), "Constant")]
[JsonSubtypes.FallBackSubType(typeof(UnknownExpression))]
public interface IExpression
{
    string Type { get; }
}
```

Or with code configuration:
```cs
settings.Converters.Add(JsonSubtypesConverterBuilder
    .Of(typeof(IExpression), "Type")
    .SetFallbackSubtype(typeof(UnknownExpression))
    .RegisterSubtype(typeof(ConstantExpression), "Constant")
    .Build());
```
```cs
settings.Converters.Add(JsonSubtypesWithPropertyConverterBuilder
    .Of(typeof(IExpression))
    .SetFallbackSubtype(typeof(UnknownExpression))
    .RegisterSubtype(typeof(ConstantExpression), "Value")
    .Build());
```

## 💖 Support this project
If this project helped you save money or time or simply makes your life also easier, you can give me a cup of coffee =)

- [![Support via PayPal](https://cdn.rawgit.com/twolfson/paypal-github-button/1.0.0/dist/button.svg)](https://www.paypal.me/manuc66)
- Bitcoin — You can send me bitcoins at this address: `33gxVjey6g4Beha26fSQZLFfWWndT1oY3F`


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes?ref=badge_large)