# __JsonSubTypes__
__JsonSubTypes__ is a discriminated Json sub-type Converter implementation for .NET

[![CI](https://github.com/manuc66/JsonSubTypes/actions/workflows/build.yml/badge.svg)](https://github.com/manuc66/JsonSubTypes/actions/workflows/build.yml)
[![CodeQL](https://github.com/manuc66/JsonSubTypes/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/manuc66/JsonSubTypes/security/code-scanning)
[![Code Coverage](https://codecov.io/gh/manuc66/JsonSubTypes/branch/master/graph/badge.svg)](https://codecov.io/gh/manuc66/JsonSubTypes)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=manuc66_JsonSubTypes&metric=alert_status)](https://sonarcloud.io/dashboard?id=manuc66_JsonSubTypes)
[![NuGet](https://img.shields.io/nuget/v/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes/)
[![NuGet](https://img.shields.io/nuget/dt/JsonSubTypes.svg)](https://www.nuget.org/packages/JsonSubTypes)
[![CodeFactor](https://www.codefactor.io/repository/github/manuc66/JsonSubTypes/badge)](https://www.codefactor.io/repository/github/manuc66/JsonSubTypes)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes?ref=badge_shield)

## Which package? State and choices

`JsonSubTypes` exists in two packages that share the same API and registration model (attributes and `JsonSubtypesConverterBuilder`):

- **`JsonSubTypes`** — for `Newtonsoft.Json`, the original and stable package.
- **`JsonSubTypes.Text.Json`** (`.NET 8+`) — for `System.Text.Json`. **Experimental**: the API is complete and the code fully tested, but the stable `1.0.0` release is still pending.

The examples below use the Newtonsoft.Json package; the API is the same for `System.Text.Json`, so read them either way. If you are targeting `System.Text.Json`, then after these examples jump to the [System.Text.Json variant](#systemtextjson-variant) section, which explains the engines available there (`Build()` converter, `BuildResolver()`, AOT generator) and their differences and limitations.

> **Security:** unless a subtype mapping is explicitly declared, the converter resolves subtypes by *name* from the JSON discriminator (only types assignable from the base are considered). See the [security section](#security) before exposing a name-based hierarchy to untrusted JSON.

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

### Native resolver via `BuildResolver()`

`JsonSubtypesConverterBuilder` also exposes the native `System.Text.Json` polymorphic contract model (`JsonPolymorphismOptions`) as an alternative to `Build()`. Assign the result to `JsonSerializerOptions.TypeInfoResolver` instead of `Converters`:

```csharp
var options = new JsonSerializerOptions
{
    TypeInfoResolver = JsonSubtypesConverterBuilder
        .Of(typeof(Animal), "type")
        .RegisterSubtype(typeof(Cat), AnimalType.Cat)
        .RegisterSubtype(typeof(Dog), AnimalType.Dog)
        .SerializeDiscriminatorProperty()
        .BuildResolver()
};
```

The resolver delegates all serialization work to `System.Text.Json`, so it only supports a subset of the converter configuration and throws at build time otherwise: `string` or `int` discriminator values, a single level of hierarchy per base type, and the discriminator always written first. The following native behaviors are exposed as opt-in builder methods:

- `FallBackToNearestAncestor()`: an unregistered derived type is serialized as its nearest registered ancestor instead of throwing.
- `IgnoreUnrecognizedTypeDiscriminators()`: an unknown type discriminator falls back to the base type instead of throwing. `SetFallbackSubtype(baseType)` enables the same behavior.
- When no subtype is registered explicitly, `[KnownSubType]` and `[FallBackSubType]` attributes on the base type are honored.

For several base type hierarchies, combine builders with `JsonSubtypesConverterBuilder.BuildResolvers(...)`. Combining resolvers through `JsonSerializerOptions.TypeInfoResolverChain` does not work, because each resolver answers for every type and only the first one would be applied.

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
- **Security**: see the [security section](#security) at the bottom of this section. It applies to both packages; the only difference is the set of assemblies searched for a name-based hit.
- The property-presence builder (`JsonSubtypesWithPropertyConverterBuilder`) registers subtypes by property name, so two subtypes cannot share the same property name through the builder (use `[KnownSubTypeWithProperty]` attributes for that case).

### Security

When a subtype is resolved by *name* — which happens for both packages **only when no subtype mapping is declared at all** (no `[KnownSubType]` attribute, no `RegisterSubtype` builder call) — the converter turns the JSON discriminator string into a type name and instantiates the matching type. Declaring a mapping at all switches the converter to that mapping, even when no entry matches; the name-based path is never used then.

Only types assignable from the polymorphic base type can be resolved, but any such type present in the base type's assembly (for Newtonsoft.Json) or in that assembly plus any assembly registered via `JsonSubTypesTypeResolution` (for `System.Text.Json`) can be instantiated with attacker-controlled JSON. Do **not** expose a name-based hierarchy to untrusted JSON without validating the payload upstream; prefer explicit `[KnownSubType]` or builder mappings whenever the discriminator can come from outside your own code.

### Which engine should I use?

`JsonSubTypes.Text.Json` ships three engines that share the same configuration layer (the attributes and `JsonSubtypesConverterBuilder`), and a parity test battery keeps them aligned:

| Feature / Capability | Native STJ (`[JsonDerivedType]`) | Resolver (`BuildResolver()`) | Converter (`Build()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | :---: | :---: | :---: | :---: |
| Type discriminator mapping (string/int) | ✅ | ✅ | ✅ | ✅ |
| Enum / `null` discriminator values | ❌ | ❌ | ✅ | ✅ |
| Custom discriminator property name | ✅ | ✅ | ✅ | ✅ |
| Property presence matching (`KnownSubTypeWithProperty`) | ❌ | ❌ | ✅ | ✅ |
| Fallback subtype (`FallBackSubType`) | ❌ | base only | ✅ | ✅ |
| Discriminator written last | ❌ | ❌ | ✅ | ✅ |
| Naming policy / case-insensitive on the discriminator name | ❌ | ⚠️ | ✅ | ✅ |
| Dotted / nested discriminator path (`"nested.type"`) | ❌ | ❌ | ✅ | ✅ |
| Nested (multi-level) hierarchies | ⚠️ | ❌ | ✅ | ✅ |
| Dynamic subtype registration at runtime | ❌ | ❌ | ✅ | ✅ (runtime map) |
| Custom type-name resolution hook | ❌ | ❌ | ✅ (built-in) | ✅ (hook) |
| Cross-assembly / plugin types outside the compilation | ❌ | ❌ | ✅ | ⚠️ (must be in the source-gen context) |
| Native AOT / Trimming support | ✅ | ❌ | ❌ | ✅ |

**The three engines in one line:**

1. **Converter (`Build()`)** — the full-featured runtime engine and the right default for non-AOT applications.
2. **Resolver (`BuildResolver()`)** — the thin native bridge: simplest and fastest, but limited to the subset the native contract model can express.
3. **Generator (`JsonSubTypes.Text.Json.Aot`)** — a Roslyn source generator emitting compiled converters: the Native AOT answer, with routing compiled instead of reflected.

**The decisive difference is not speed, it is when the hierarchy is known:**

| | Converter (`Build()`) | Generator (`JsonSubTypes.Text.Json.Aot`) |
| :--- | :--- | :--- |
| Subtypes known at **compile time** (attributes on your own types) | ✅ | ✅ |
| Subtypes known only at **runtime** (plugins, loaded assemblies, config) | ✅ | ✅ (via `RegisterDynamicSubtype` / resolver hooks) |
| Subtypes in **third-party assemblies** you cannot annotate | ✅ (builder, no attribute needed) | ❌ (generator only sees the source-gen context) |

The generator reads its registrations from `[JsonSubTypesAotConverter]`/`[KnownSubType]`-style **attributes at compile time** (`JsonSubTypesGenerator.cs`). It can only route types visible to the compilation it runs in. The converter's `Build()` accepts a **runtime** registration through the builder, so it is the only engine that can handle hierarchies whose subtypes are discovered at runtime — plugins, assemblies loaded dynamically, or types you do not own. The generator is the better fit when the hierarchy is fixed and known at build time, and the only engine compatible with trimming/Native AOT.

### Converter known scope & fallback path

To preserve full compatibility with advanced features while delegating object serialization to `System.Text.Json`, the converter isolates base-type serialization to a narrow path (when serializing the base type directly or reading an unregistered fallback type):

- **Subtypes (most cases)**: Full delegation to `System.Text.Json`. STJ attributes (`[JsonIgnore]`, `[JsonInclude]`, property `[JsonConverter]`, `[JsonConstructor]`, `record` types, naming policies) are fully supported natively.
- **Base-as-leaf & Fallback path**: lightweight direct property mapping honoring `[JsonIgnore]`, `[JsonPropertyName]`, the naming policy and `PropertyNameCaseInsensitive`. Per-property `[JsonConverter]`, `[JsonInclude]` fields, `required` members and parameterized constructors are not re-implemented on this path.
- **Parameterless constructor required** for the base fallback type. Subtypes resolved via the discriminator support all STJ constructor features (primary constructors, `record` types).

### Performance (measured)

Benchmarked with BenchmarkDotNet (`JsonSubTypes.Benchmarks`, .NET 10); the methodology, machine and full result tables are in [PERFORMANCE.md](PERFORMANCE). In short:

- **Resolver (`BuildResolver()`)** is the fastest: it delegates to `System.Text.Json` native polymorphism, with no `JsonDocument` round-trip and no reflection per call.
- **Generator (`JsonSubTypes.Text.Json.Aot`)** beats the runtime converter on deserialization and allocates far less (compiled routing instead of per-call converter scans). Its Native AOT steady state is comparable to (slightly slower than) JIT; its real advantage is trimming compatibility and startup time.
- **Converter (`Build()`)** is the slowest of the three: it keeps the `JsonDocument` round-trip and adds runtime type resolution. It is the only engine for hierarchies whose subtypes are only known at runtime.
- **Newtonsoft.Json (`JsonSubTypes`)** is slower and allocates several times more than the STJ converter on the same scenarios.

Reproduce the measurements yourself with `dotnet run -c Release --project JsonSubTypes.Benchmarks`.

### Decision matrix
| Use case | Recommended |
|---|---|
| Native AOT / trimming, hierarchy known at compile time | `JsonSubTypes.Text.Json.Aot` generator |
| Non-AOT, full feature set with minimal setup | Converter (`Build()`) |
| Non-AOT, string/int discriminators only, fastest and simplest | Resolver (`BuildResolver()`) |
| Discriminator by property presence (no discriminator field in the JSON) | Converter or Generator |
| Open hierarchies / subtypes registered at runtime | Converter, or Generator (`RegisterDynamicSubtype`) |
| Non string/int discriminator values (enums, `null`) | Converter or Generator |
| Nested or dotted discriminator paths (e.g. `"nested.property"`) | Converter or Generator |
| Resolution by arbitrary .NET type name / cross-assembly plugins | Converter (built-in), or Generator (`CustomTypeNameResolver` hook) |
| Migrating an existing JsonSubTypes/Newtonsoft code base | Converter (same API) |

### Native AOT

The resolver and the converter rely on reflection and are therefore **not compatible with trimming or Native AOT**. The polymorphic metadata that the resolver configures must be declared at compile time for AOT: `System.Text.Json` freezes it at build time, and a source-generated `JsonTypeInfo` is read-only at runtime. Assigning `PolymorphismOptions` to a source-generated `JsonTypeInfo` throws `InvalidOperationException` on both .NET 8 and .NET 10.

For Native AOT, the `JsonSubTypes.Text.Json.Aot` generator compiles the routing into the converter (verified to run as a native binary with `dotnet publish -r linux-x64 -p:PublishAot=true`). The generator is referenced as an analyzer and reads its attributes (`[JsonSubTypesAotConverter]`, `[KnownSubType]`, …) from the `JsonSubTypes.Text.Json` package, so reference **both** `JsonSubTypes.Text.Json.Aot` and `JsonSubTypes.Text.Json`:

```bash
dotnet add package JsonSubTypes.Text.Json.Aot
dotnet add package JsonSubTypes.Text.Json
```

Alternatively, declare the hierarchy with `[JsonDerivedType]` on the base type and use a plain source-generated context:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Circle), "circle")]
[JsonDerivedType(typeof(Square), "square")]
public class Shape { }

[JsonSerializable(typeof(Shape))]
[JsonSerializable(typeof(Circle))]
[JsonSerializable(typeof(Square))]
public partial class ShapeJsonContext : JsonSerializerContext { }

var options = new JsonSerializerOptions { TypeInfoResolver = ShapeJsonContext.Default };
var json = JsonSerializer.Serialize<Shape>(new Circle { Radius = 2 }, options);
// {"$type":"circle","Radius":2}
```
## 💖 Support this project
If this project helped you save money or time or simply makes your life also easier, you can give me a cup of coffee =)

- [![Support via PayPal](https://cdn.rawgit.com/twolfson/paypal-github-button/1.0.0/dist/button.svg)](https://www.paypal.me/manuc66)
- Bitcoin — You can send me bitcoins at this address: `33gxVjey6g4Beha26fSQZLFfWWndT1oY3F`


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fmanuc66%2FJsonSubTypes?ref=badge_large)