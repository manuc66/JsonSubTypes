# __JsonSubTypes__ a discriminated Json sub-type Converter implementation for .NET
[![Build status](https://ci.appveyor.com/api/projects/status/g11crbl037en6rkq/branch/master?svg=true)](https://ci.appveyor.com/project/manuc66/jsonsubtypes/branch/master)
[![Code Coverage](https://codecov.io/gh/manuc66/JsonSubTypes/branch/master/graph/badge.svg)](https://codecov.io/gh/manuc66/JsonSubTypes)
[![Quality Gate](https://sonarqube.com/api/badges/gate?key=manuc66:JsonSubtypes:master)](https://sonarqube.com/dashboard/index/manuc66:JsonSubtypes:master)
[![NuGet](https://img.shields.io/nuget/v/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes/)

## DeserializeObject with custom type property name

```csharp
[JsonConverter(typeof(JsonSubtypes), "Kind")]
public interface IAnnimal
{
    string Kind { get; }
}

public class Dog : IAnnimal
{
    public string Kind { get; } = "Dog";
    public string Breed { get; set; }
}

class Cat : Annimal {
    bool declawed { get; set;}
}
```

```csharp
var annimal =JsonConvert.DeserializeObject<IAnnimal>("{\"Kind\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
```
N.B.: Also works with fully qualified type name

## DeserializeObject with custom type mapping

```csharp
[JsonConverter(typeof(JsonSubtypes), "Sound")]
[JsonSubtypes.KnownSubType(typeof(Dog), "Bark")]
[JsonSubtypes.KnownSubType(typeof(Cat), "Meow")]
public class Annimal
{
    public virtual string Sound { get; }
    public string Color { get; set; }
}

public class Dog : Annimal
{
    public override string Sound { get; } = "Bark";
    public string Breed { get; set; }
}

public class Cat : Annimal
{
    public override string Sound { get; } = "Meow";
    public bool Declawed { get; set; }
}
```

```csharp
var annimal =JsonConvert.DeserializeObject<IAnnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
```

N.B.: Also works with other kind of value than string, i.e.: enums, int, ...

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
