namespace JsonSubTypes.Text.Json.Tests.Shared
{
    [JsonSubTypeConverter(typeof(JsonSubtypes<SharedAnimal>), "Kind")]
    [JsonSubTypesTypeResolution("JsonSubTypes.Text.Json.Tests.Plugin")]
    public class SharedAnimal
    {
        public string? Kind { get; set; }
    }
}
