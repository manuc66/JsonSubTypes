namespace JsonSubTypes.Text.Json.Tests.Shared
{
    [JsonSubTypeConverter(typeof(JsonSubtypes<SharedAnimal>), "Kind")]
    public class SharedAnimal
    {
        public string? Kind { get; set; }
    }
}
