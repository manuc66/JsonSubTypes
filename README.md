# __JsonSubTypes__
__JsonSubTypes__ is a discriminated Json sub-type Converter implementation for .NET

[![Build status](https://ci.appveyor.com/api/projects/status/g11crbl037en6rkq/branch/master?svg=true)](https://ci.appveyor.com/project/manuc66/jsonsubtypes/branch/master)
[![Code Coverage](https://codecov.io/gh/manuc66/JsonSubTypes/branch/master/graph/badge.svg)](https://codecov.io/gh/manuc66/JsonSubTypes)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=manuc66_JsonSubTypes&metric=alert_status)](https://sonarcloud.io/dashboard?id=manuc66_JsonSubTypes)
[![NuGet](https://img.shields.io/nuget/v/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes/)
[![NuGet](https://img.shields.io/nuget/dt/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes)
[![CodeFactor](https://www.codefactor.io/repository/github/manuc66/JsonSubTypes/badge)](https://www.codefactor.io/repository/github/manuc66/JsonSubTypes)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes?ref=badge_shield)

> **Note:** this library is built around `Json.NET`/`Newtonsoft.Json` — that is where its API and reputation come from, and the `JsonSubTypes` NuGet package targets it. A `System.Text.Json` port exists as the `JsonSubTypes.Text.Json` package (`.NET 8+`): it shares the same API but is **experimental**. Full documentation, differences and known limitations are in the dedicated section at the bottom: [System.Text.Json variant](#systemtextjson-variant).


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

## System.Text.Json variant

> **Status: experimental.** The `JsonSubTypes.Text.Json` package is a **release candidate** (`1.0.0-rc.x`) and not yet part of the project's stable offering. The code is fully tested (133 unit tests) and the API is complete, but the stable `1.0.0` release will follow once the package has been exercised in more real-world projects.

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

Like the native `[JsonDerivedType]` polymorphism, the attribute-based converter handles **both directions**: serializing through the base type writes the discriminator, and deserialization reads it back, so round-trips work out of the box:

```csharp
var json = JsonSerializer.Serialize<Animal>(new Dog { Breed = "Jack Russell Terrier" });
// {"Sound":"Bark","Breed":"Jack Russell Terrier"}
var back = JsonSerializer.Deserialize<Animal>(json);
Assert.IsInstanceOf<Dog>(back);
```

When the runtime type is not declared in the `[KnownSubType]` mappings (e.g. a multi-level hierarchy where the leaf is registered on an intermediate base), serialization falls back to the plain runtime-type contract without a discriminator.

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

### Serializing the discriminator

The attribute-based converter writes the discriminator by default. For the builder, writing the discriminator is opt-in, like the Newtonsoft version:

```csharp
options.Converters.Add(JsonSubtypesConverterBuilder
    .Of(typeof(Animal), "type")
    .SerializeDiscriminatorProperty()                 // discriminator first (default)
    // or .SerializeDiscriminatorProperty(false)      // discriminator last
    .RegisterSubtype(typeof(Cat), AnimalType.Cat)
    .RegisterSubtype(typeof(Dog), AnimalType.Dog)
    .Build());

var json = JsonSerializer.Serialize<Animal>(new Cat { Age = 11, Lives = 6 }, options);
// {"type":2,"catLives":6,"age":11}
```

As with the native `[JsonDerivedType]` polymorphism, serialization must go through the **base type** (or a base-typed property/collection) for the converter and the discriminator to apply. Serializing a value with a concrete subtype as its static type bypasses the converter, and serializing an unregistered type throws when `SerializeDiscriminatorProperty()` is used.

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

- The attribute-based converter writes the discriminator by default (like the native `[JsonDerivedType]` polymorphism), whereas the Newtonsoft version never writes it from attributes (`CanWrite = false`). With the builder, writing is opt-in via `SerializeDiscriminatorProperty()`.
- With `System.Text.Json`, the converter is only applied when the static type is the polymorphic base type (or a base-typed property/collection), matching the native `[JsonDerivedType]` behavior. The Newtonsoft version also applies converters when serializing a value whose static type is a concrete subtype.
- A property declared with a base class or interface type is serialized using the **declared type's contract**: subtype members are omitted unless a converter that claims the declared type is applied (attribute on the type, or builder registered in `JsonSerializerOptions`). The Newtonsoft version serialized the runtime type by default.
- Property order differs: `System.Text.Json` emits properties most-derived-first, while the Newtonsoft version honored `[JsonProperty(Order = N)]`. There is no `Order` support in `System.Text.Json`.
- Deeply nested graphs need `MaxDepth` about one level higher than with the Newtonsoft/plain serialization: the discriminator write path round-trips through a `JsonDocument`, which consumes one depth level. (A 64-level chain requires `MaxDepth = 66` instead of 65.)
- Name-based type resolution stays scoped to the base type's assembly by default. Cross-assembly subtypes require an explicit opt-in: `JsonSubTypesTypeResolution.AddAssembly(...)`, a capability the Newtonsoft version does not have.
- `JsonNamingPolicy` and `PropertyNameCaseInsensitive` are respected when matching the discriminator property, and `JsonStringEnumConverter` is respected when mapping discriminator values. Note that `JsonStringEnumConverter` (.NET 8) does **not** honor `[EnumMember(Value = ...)]` — use enum names or `[JsonStringEnumMemberName]` (.NET 9+).
- Dotted or nested discriminator property paths (e.g. `"nested.property"`) are supported.
- **Fallback paths**: serializing the base type itself (rather than a subtype) and deserializing an unknown discriminator back to the base use a reflection-based writer/reader, because the base type's contract is owned by the converter (`System.Text.Json` exposes no property metadata for converter-owned types). `[JsonPropertyName]`, `[JsonIgnore]` (including `JsonIgnoreCondition`), the naming policy and `DefaultIgnoreCondition` are honored; per-property `[JsonConverter]`, `[JsonInclude]` fields, `required` members and parameterized constructors are not supported on these two paths.
- **Performance**: writing an object with a discriminator serializes it once, then re-parses the JSON (`JsonDocument`) to inject the discriminator property, so payloads spend roughly 2-3x their size in temporary memory on the write path. This is the cost of the converter architecture and of the `MaxDepth + 1` note above.
- **Security**: name-based subtype resolution (`GetTypeByName`, used when no `[KnownSubType]` mapping is declared) resolves a type name from the JSON discriminator against the base type's assembly (and any assembly registered via `JsonSubTypesTypeResolution`). Only types assignable from the base can be resolved, but do **not** expose a name-based hierarchy to untrusted JSON without validating the payload upstream.
- The property-presence builder (`JsonSubtypesWithPropertyConverterBuilder`) registers subtypes by property name, so two subtypes cannot share the same property name through the builder (use `[KnownSubTypeWithProperty]` attributes for that case).

### Native `[JsonDerivedType]` vs `JsonSubTypes.Text.Json`

| Feature / Capability | Native STJ (`[JsonDerivedType]`) | `JsonSubTypes.Text.Json` |
| :--- | :---: | :---: |
| Type discriminator mapping | ✅ | ✅ |
| Custom discriminator property name | ✅ | ✅ |
| Property presence matching (`KnownSubTypeWithProperty`) | ❌ | ✅ |
| Fallback subtype (`FallBackSubType`) | ❌ | ✅ |
| Cross-assembly / Plugin type resolution | ❌ | ✅ |
| Dotted / nested discriminator path (`"nested.type"`) | ❌ | ✅ |
| Opt-in discriminator writing (`SerializeDiscriminatorProperty`) | ❌ | ✅ |
| Seamless migration from `Newtonsoft.Json` `JsonSubTypes` | ❌ | ✅ |
| Native AOT / Trimming support | ✅ | ⚠️ (Requires reflection) |

### Known Scope & Fallback Path Behavior

To preserve full compatibility with advanced features (`KnownSubTypeWithProperty`, nested discriminator paths, enum/null discriminators, cross-assembly resolution) while delegating 99% of object serialization to `System.Text.Json`, the library isolates base-type serialization to two narrow paths (when serializing the base type directly or reading an unregistered fallback type):

1. **Subtypes (99% of cases)**: Full delegation to `System.Text.Json`. All STJ attributes (`[JsonIgnore]`, `[JsonInclude]`, property `[JsonConverter]`, `[JsonConstructor]`, `record` types, naming policies) are fully supported natively.
2. **Base-as-leaf & Fallback path**: Handled via lightweight direct property mapping. Standard attributes (`[JsonIgnore]`, `[JsonPropertyName]`, `PropertyNamingPolicy`, `PropertyNameCaseInsensitive`) are honored. Advanced member-level STJ attributes (e.g. `[JsonInclude]` on fields, `[JsonConverter]` on individual base properties, parameterized constructors) on the fallback base type itself are intentionally not re-implemented to avoid duplicate serializer engine complexity.

- **Parameterless Constructor for Fallback**: The base fallback type requires a parameterless constructor. Subtypes resolved via discriminator mapping support all STJ constructor features (primary constructors, `record` types).
- **Native AOT**: Relies on reflection to discover subtypes; annotated with `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`.

### Native `[JsonDerivedType]` or `JsonSubTypes.Text.Json`?

| Use case | Recommended |
|---|---|
| Closed hierarchy, all subtypes known at compile time, string/int discriminator, round-trip serialization, Native AOT | Native `[JsonDerivedType]` / `[JsonPolymorphic]` (source-gen friendly) |
| Discriminator by property presence (no discriminator field in the JSON) | `JsonSubTypes.Text.Json` |
| Open hierarchies / subtypes registered at runtime | `JsonSubTypes.Text.Json` |
| Non string/int discriminator values (enums, `null`, several values mapping to one type) | `JsonSubTypes.Text.Json` |
| Nested or dotted discriminator paths (e.g. `"nested.property"`) | `JsonSubTypes.Text.Json` |
| Resolution by .NET type name, or cross-assembly plugin subtypes | `JsonSubTypes.Text.Json` |
| Migrating an existing JsonSubTypes/Newtonsoft code base | `JsonSubTypes.Text.Json` (same API) |
## 💖 Support this project
If this project helped you save money or time or simply makes your life also easier, you can give me a cup of coffee =)

- [![Support via PayPal](https://cdn.rawgit.com/twolfson/paypal-github-button/1.0.0/dist/button.svg)](https://www.paypal.me/manuc66)
- Bitcoin — You can send me bitcoins at this address: `33gxVjey6g4Beha26fSQZLFfWWndT1oY3F`


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes?ref=badge_large)