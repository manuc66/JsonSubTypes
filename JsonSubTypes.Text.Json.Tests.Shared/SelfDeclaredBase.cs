namespace JsonSubTypes.Text.Json.Tests.Shared
{
    // A base type without a [JsonSubTypeConverter] attribute or a KnownSubTypeOtherAssembly:
    // used to verify the self-declaring plugin pattern, where the subtype registers itself and
    // the host registers the plugin assembly at runtime.
    public class SelfDeclaredBase
    {
        public string? Kind { get; set; }
    }
}
