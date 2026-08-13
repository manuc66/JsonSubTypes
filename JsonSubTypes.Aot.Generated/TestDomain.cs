#nullable enable
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

namespace JsonSubTypes.Aot.Generated.TestDomain
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
}
