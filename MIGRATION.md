# Migration guide

How to move between the packages and engines. Each section shows the same scenario before and after, and lists what actually changes in behaviour — not what would be nice to change.

## Newtonsoft.Json → JsonSubTypes.Text.Json (converter)

The two packages share the same configuration model (attributes and `JsonSubtypesConverterBuilder`), so most code moves over line by line.

**Before (Newtonsoft.Json):**

```csharp
[JsonConverter(typeof(JsonSubtypes), "Kind")]
[JsonSubtypes.KnownSubType(typeof(Dog), "Dog")]
[JsonSubtypes.KnownSubType(typeof(Cat), "Cat")]
public class Animal
{
    public virtual string Kind { get; }
}

// registration / usage
JsonConvert.DeserializeObject<Animal>("{\"Kind\":\"Dog\",\"Breed\":\"Rex\"}");
```

**After (System.Text.Json):**

```csharp
[JsonSubTypeConverter(typeof(JsonSubtypes<Animal>), "Kind")]
[KnownSubType(typeof(Dog), "Dog")]
[KnownSubType(typeof(Cat), "Cat")]
public class Animal
{
    public virtual string Kind { get; }
}

// registration / usage: options are required (STJ has no DefaultSettings)
var options = new JsonSerializerOptions();
options.Converters.Add(JsonSubtypesConverterBuilder.Of<Animal>("Kind").Build());

JsonSerializer.Deserialize<Animal>("{\"Kind\":\"Dog\",\"Breed\":\"Rex\"}", options);
```

Mechanical differences:

- `[JsonConverter(typeof(JsonSubtypes), "Kind")]` becomes `[JsonSubTypeConverter(typeof(JsonSubtypes<Animal>), "Kind")]` — the STJ converter is generic over the base type.
- `[JsonSubtypes.KnownSubType]` becomes `[KnownSubType]` (import `JsonSubTypes.Text.Json`).
- `JsonConvert.SerializeObject`/`DeserializeObject` become `JsonSerializer.Serialize`/`Deserialize`, and you must pass a `JsonSerializerOptions` (there is no equivalent of `DefaultSettings`).
- The builder (`JsonSubtypesConverterBuilder.Of(...)`, `RegisterSubtype`, `SerializeDiscriminatorProperty`) is the same shape. `JsonSubtypesWithPropertyConverterBuilder` likewise.

Behaviour that actually differs — check your tests against these:

- **The attribute-based STJ converter writes the discriminator by default**; the Newtonsoft one never does from attributes (`CanWrite = false`). If you relied on attributes for read-only, the JSON shape changes.
- **The converter applies only when the static type is the base type.** A property declared with a base/interface type serializes with the declared type's contract; subtype members are omitted unless a converter claims the declared type. Newtonsoft serialized the runtime type by default.
- **Property order differs.** STJ emits most-derived-first; there is no `[JsonProperty(Order = N)]` support.
- **`MaxDepth` needs one more level** because the write path round-trips through a `JsonDocument`.
- **Fallback paths are narrower**: serializing the base type directly or an unknown discriminator uses a reflection-based path that honors `[JsonIgnore]`, `[JsonPropertyName]`, naming policy and `DefaultIgnoreCondition`, but not per-property `[JsonConverter]`, `[JsonInclude]` fields, `required` members or parameterized constructors.
- **Cross-assembly subtypes** require opt-in (`JsonSubTypesTypeResolution.AddAssembly`); Newtonsoft never supported them.
- **Security**: the name-based resolution warning in the README applies to both; see the [security section](./#security) there.

## Between the System.Text.Json engines

The three engines share the configuration layer (attributes + builder), but they do not support the same feature set. Moving between them is usually a change of *capability*, not just a change of call.

### Converter (`Build()`) → Resolver (`BuildResolver()`)

The resolver delegates to `System.Text.Json` native polymorphism. It is faster but refuses configuration it cannot express — at build time (`NotSupportedException`), not silently. Before moving, check that your scenario only uses:

- string or int discriminator values (no enum, no `null`, no other value types),
- a single level of hierarchy per base type (no nested multi-level chains),
- the discriminator always written first,
- a fallback only to the base type (`SetFallbackSubtype(baseType)` maps to ignore-unrecognized; anything else throws).

```csharp
// before
var options = new JsonSerializerOptions();
options.Converters.Add(JsonSubtypesConverterBuilder
    .Of<Animal>("Kind").RegisterSubtype<Dog>("Dog").Build());

// after
var options = new JsonSerializerOptions
{
    TypeInfoResolver = JsonSubtypesConverterBuilder
        .Of<Animal>("Kind").RegisterSubtype<Dog>("Dog").BuildResolver()
};
```

Property-presence matching, dotted discriminator paths, enum/`null` discriminators and multi-level hierarchies are not supported by the resolver — keep the converter for those.

### Converter (`Build()`) → Generator (`JsonSubTypes.Text.Json.Aot`)

The generator reads its registrations from attributes at compile time and can only route types visible to the compilation. Move to it when the hierarchy is fixed and known at build time (or you publish with Native AOT / trimming), and reference **both** packages:

```bash
dotnet add package JsonSubTypes.Text.Json.Aot
dotnet add package JsonSubTypes.Text.Json
```

```csharp
// before (builder)
var options = new JsonSerializerOptions();
options.Converters.Add(JsonSubtypesConverterBuilder
    .Of<Animal>("Kind").RegisterSubtype<Dog>("Dog").Build());

// after (attributes + generated converter)
[JsonSubTypesAotConverter("Kind")]
[KnownSubType(typeof(Dog), "Dog")]
public class Animal { }

var options = new JsonSerializerOptions
{
    TypeInfoResolver = MyContext.Default,
    Converters = { JsonSubTypesAotConverters.Animal }
};
[JsonSerializable(typeof(Animal))]
[JsonSerializable(typeof(Dog))]
public partial class MyContext : JsonSerializerContext { }
```

If you do not own the types (plugins, third-party assemblies) or the subtypes are only known at runtime, the generator cannot see them — keep the converter (or use `RegisterDynamicSubtype` where supported).

### Resolver → Generator

Both are compile-time friendly, but the generator supports strictly more features (enums, `null`, property presence, fallback subtypes, discriminator written last, nested hierarchies). If you outgrow the resolver's subset, move to the generator rather than back to the converter — the configuration layer (attributes) is the same, so the change is mostly the call site.

## What is not covered by this guide

- `System.Text.Json` native polymorphism (`[JsonDerivedType]` + `[JsonPolymorphic]`) — that is a different API entirely, covered in the README's [System.Text.Json variant](./#systemtextjson-variant) section.
- Serialization of dates, GUIDs, number formats and other STJ/Newtonsoft type-level differences that are unrelated to polymorphism.
