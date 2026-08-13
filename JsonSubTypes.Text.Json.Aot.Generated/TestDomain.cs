#nullable enable
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

namespace JsonSubTypes.Text.Json.Aot.Generated.TestDomain
{
    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(Cat), "cat")]
    [KnownSubType(typeof(Dog), 2)]
    public class Animal
    {
        public int Age { get; set; }
    }

    public class Cat : Animal
    {
        public int Lives { get; set; }
    }

    public class Dog : Animal
    {
        public bool CanHunt { get; set; }
    }

    public class Fox : Animal
    {
        public int Speed { get; set; }
    }

    public class Owl : Animal
    {
        public int Wingspan { get; set; }
    }

    [JsonSubTypesAotConverter]
    [KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
    [KnownSubTypeWithProperty(typeof(Artist), "Skill")]
    [FallBackSubType(typeof(Person))]
    public class Person
    {
        public string? FirstName { get; set; }
    }

    public class Employee : Person
    {
        public string? JobTitle { get; set; }
    }

    public class Artist : Person
    {
        public string? Skill { get; set; }
    }

    [JsonSubTypesAotConverter("kind")]
    [KnownSubType(typeof(ElectronicCat), GadgetKind.ElectronicCat)]
    public class Gadget
    {
        public int Age { get; set; }
    }

    public class ElectronicCat : Gadget
    {
        public int Lives { get; set; }
    }

    public enum GadgetKind
    {
        ElectronicCat
    }

    [JsonSubTypesAotConverter("nested.type")]
    [KnownSubType(typeof(DottedElectronic), "electronic")]
    public class DottedGadget
    {
        public int Age { get; set; }
    }

    public class DottedElectronic : DottedGadget
    {
        public int Lives { get; set; }
    }

    public enum PayloadDiscriminator
    {
        GAME = 0,
        COM = 1
    }

    public enum GameDiscriminator
    {
        RUN = 0,
        WALK = 1
    }

    [JsonSubTypesAotConverter("$PayloadKind")]
    [KnownSubType(typeof(Game), PayloadDiscriminator.GAME)]
    [KnownSubType(typeof(Com), PayloadDiscriminator.COM)]
    public class Payload
    {
    }

    public class Com : Payload
    {
    }

    [JsonSubTypesAotConverter("$GameKind")]
    [KnownSubType(typeof(Run), GameDiscriminator.RUN)]
    [KnownSubType(typeof(Walk), GameDiscriminator.WALK)]
    public class Game : Payload
    {
    }

    public class Run : Game
    {
    }

    public class Walk : Game
    {
    }

    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(NullDiscriminatorAnimal), null)]
    [KnownSubType(typeof(Deer), "deer")]
    public class NullDiscriminatorAnimal
    {
        public int Age { get; set; }
    }

    public class Deer : NullDiscriminatorAnimal
    {
        public int AntlerSize { get; set; }
    }

    [JsonSubTypesAotConverter("type", AddDiscriminatorFirst = false)]
    [KnownSubType(typeof(Mammoth), "mammoth")]
    public class DiscriminatorLast
    {
        public int Age { get; set; }
    }

    public class Mammoth : DiscriminatorLast
    {
        public int Tusks { get; set; }
    }

    [JsonSubTypesAotConverter("kind")]
    [KnownSubType(typeof(DynamicCat), "cat")]
    public class DynamicShape
    {
        public int Age { get; set; }

        public string Computed { get; } = "computed";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Nickname { get; set; }
    }

    public class DynamicCat : DynamicShape
    {
        public int Lives { get; set; }
    }

    [JsonSerializable(typeof(Animal))]
    [JsonSerializable(typeof(Cat))]
    [JsonSerializable(typeof(Dog))]
    [JsonSerializable(typeof(Fox))]
    [JsonSerializable(typeof(GadgetKind))]
    [JsonSerializable(typeof(PayloadDiscriminator))]
    [JsonSerializable(typeof(GameDiscriminator))]
    [JsonSerializable(typeof(Person))]
    [JsonSerializable(typeof(Employee))]
    [JsonSerializable(typeof(Artist))]
    [JsonSerializable(typeof(Gadget))]
    [JsonSerializable(typeof(ElectronicCat))]
    [JsonSerializable(typeof(DottedGadget))]
    [JsonSerializable(typeof(DottedElectronic))]
    [JsonSerializable(typeof(Payload))]
    [JsonSerializable(typeof(Com))]
    [JsonSerializable(typeof(Game))]
    [JsonSerializable(typeof(Run))]
    [JsonSerializable(typeof(Walk))]
    [JsonSerializable(typeof(NullDiscriminatorAnimal))]
    [JsonSerializable(typeof(Deer))]
    [JsonSerializable(typeof(DiscriminatorLast))]
    [JsonSerializable(typeof(Mammoth))]
    [JsonSerializable(typeof(DynamicShape))]
    [JsonSerializable(typeof(DynamicCat))]
    public partial class TestDomainJsonContext : JsonSerializerContext
    {
    }
}
