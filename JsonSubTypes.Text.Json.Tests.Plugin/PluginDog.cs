using JsonSubTypes.Text.Json;
using JsonSubTypes.Text.Json.Tests.Shared;

namespace JsonSubTypes.Text.Json.Tests.Plugin
{
    [KnownSubTypeOf(typeof(SharedAnimal), "Dog")]
    public class PluginDog : SharedAnimal
    {
        public bool CanBark { get; set; }
    }
}
