using Newtonsoft.Json;

namespace StubAssemblyForTests
{
    public interface IExtenalAnimal
    {
        [JsonProperty("age", Order = 1)]
        int Age { get; set; }
    }
}