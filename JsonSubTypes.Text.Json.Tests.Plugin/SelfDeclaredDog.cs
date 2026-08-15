using JsonSubTypes.Text.Json;
using JsonSubTypes.Text.Json.Tests.Shared;

namespace JsonSubTypes.Text.Json.Tests.Plugin
{
    // A subtype in a separate assembly that declares itself as a subtype of SelfDeclaredBase
    // through [KnownSubTypeOf]. The host registers the plugin assembly at runtime; the base type
    // knows nothing about this type or its assembly.
    [KnownSubTypeOf(typeof(SelfDeclaredBase), "Dog")]
    public class SelfDeclaredDog : SelfDeclaredBase
    {
        public bool CanBark { get; set; }
    }

    // Self-declared without a discriminator value: resolved by type name in the registered
    // assembly rather than by a discriminator value. Lives in an assembly with no value-mapped
    // subtypes, so the name-based path stays active.
    [KnownSubTypeOf(typeof(SelfDeclaredCatBase))]
    public class SelfDeclaredCat : SelfDeclaredCatBase
    {
        public bool Purrs { get; set; }
    }

    public class SelfDeclaredCatBase
    {
        public string? Kind { get; set; }
    }
}
