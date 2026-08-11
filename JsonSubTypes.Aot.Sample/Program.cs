using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Aot.Generated;
using JsonSubTypes.Text.Json;

var options = new JsonSerializerOptions
{
    TypeInfoResolver = AnimalJsonContext.Default,
    Converters = { JsonSubTypesAotConverters.Animal, JsonSubTypesAotConverters.Person, JsonSubTypesAotConverters.Gadget, JsonSubTypesAotConverters.Payload, JsonSubTypesAotConverters.Game }
};

string json = JsonSerializer.Serialize<Animal>(new Cat { Age = 11, Lives = 6 }, options);
Console.WriteLine($"serialize: {json}");

Animal? back = JsonSerializer.Deserialize<Animal>(json, options);
Console.WriteLine($"deserialize: {back?.GetType().Name}");

string presenceJson = JsonSerializer.Serialize<Person>(new Artist { Skill = "Painter", FirstName = "A" }, options);
Console.WriteLine($"presence serialize: {presenceJson}");

Person? person = JsonSerializer.Deserialize<Person>("{\"Skill\":\"Painter\",\"FirstName\":\"A\"}", options);
Console.WriteLine($"presence deserialize: {person?.GetType().Name}");

string baseJson = JsonSerializer.Serialize<Person>(new Person { FirstName = "B" }, options);
Console.WriteLine($"presence base-as-leaf serialize: {baseJson}");

Animal? fallback = JsonSerializer.Deserialize<Animal>("{\"type\":\"fish\",\"Age\":1}", options);
Console.WriteLine($"base fallback: {fallback?.GetType().Name}");

var enumOptions = new JsonSerializerOptions
{
    TypeInfoResolver = AnimalJsonContext.Default,
    Converters = { JsonSubTypesAotConverters.Gadget, new JsonStringEnumConverter() }
};
string enumJson = JsonSerializer.Serialize<Gadget>(new ElectronicCat { Age = 2, Lives = 9 }, enumOptions);
Console.WriteLine($"enum serialize: {enumJson}");
Console.WriteLine($"enum deserialize: {JsonSerializer.Deserialize<Gadget>(enumJson, enumOptions)?.GetType().Name}");

string dottedJson = JsonSerializer.Serialize<DottedGadget>(new DottedElectronic { Age = 2, Lives = 9 }, new JsonSerializerOptions
{
    TypeInfoResolver = AnimalJsonContext.Default,
    Converters = { JsonSubTypesAotConverters.DottedGadget }
});
Console.WriteLine($"dotted serialize: {dottedJson}");

DottedGadget? dotted = JsonSerializer.Deserialize<DottedGadget>("{\"nested\":{\"type\":\"electronic\"},\"Lives\":9,\"Age\":2}", new JsonSerializerOptions
{
    TypeInfoResolver = AnimalJsonContext.Default,
    Converters = { JsonSubTypesAotConverters.DottedGadget }
});
Console.WriteLine($"dotted nested deserialize: {dotted?.GetType().Name}");

string plainBaseJson = JsonSerializer.Serialize<Animal>(new Animal { Age = 1 }, options);
Console.WriteLine($"base-as-leaf without mapping (plain): {plainBaseJson}");

string nestedJson = JsonSerializer.Serialize<Payload>(new Run(), options);
Console.WriteLine($"nested serialize: {nestedJson}");
Console.WriteLine($"nested deserialize: {JsonSerializer.Deserialize<Payload>(nestedJson, options)?.GetType().Name}");

JsonSubTypesAotConverters.Gadget.RegisterDynamicSubtype("mouse", typeof(ElectronicMouse));
var dynamicOpts = new JsonSerializerOptions
{
    TypeInfoResolver = AnimalJsonContext.Default,
    Converters = { JsonSubTypesAotConverters.Gadget, new JsonStringEnumConverter() }
};
string dynamicJson = JsonSerializer.Serialize<Gadget>(new ElectronicMouse { Age = 1, Buttons = 3 }, dynamicOpts);
Console.WriteLine($"dynamic serialize: {dynamicJson}");
Console.WriteLine($"dynamic deserialize: {JsonSerializer.Deserialize<Gadget>(dynamicJson, dynamicOpts)?.GetType().Name}");

JsonSubTypesAotConverters.Gadget.CustomTypeNameResolver = name =>
    name as string == "mouse-by-name" ? typeof(ElectronicMouse) : null;
string byNameJson = JsonSerializer.Serialize<Gadget>(new ElectronicCat { Age = 2, Lives = 9 }, dynamicOpts);
Console.WriteLine($"name-based deserialize: {JsonSerializer.Deserialize<Gadget>("{\"kind\":\"mouse-by-name\",\"Buttons\":2,\"Age\":1}", dynamicOpts)?.GetType().Name}");

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

public class ElectronicMouse : Gadget
{
    public int Buttons { get; set; }
}

[JsonSerializable(typeof(Animal))]
[JsonSerializable(typeof(Cat))]
[JsonSerializable(typeof(Dog))]
[JsonSerializable(typeof(Person))]
[JsonSerializable(typeof(Employee))]
[JsonSerializable(typeof(Artist))]
[JsonSerializable(typeof(Gadget))]
[JsonSerializable(typeof(ElectronicCat))]
[JsonSerializable(typeof(GadgetKind))]
[JsonSerializable(typeof(DottedGadget))]
[JsonSerializable(typeof(DottedElectronic))]
[JsonSerializable(typeof(Payload))]
[JsonSerializable(typeof(Com))]
[JsonSerializable(typeof(Game))]
[JsonSerializable(typeof(Run))]
[JsonSerializable(typeof(Walk))]
[JsonSerializable(typeof(PayloadDiscriminator))]
[JsonSerializable(typeof(GameDiscriminator))]
[JsonSerializable(typeof(ElectronicMouse))]
public partial class AnimalJsonContext : JsonSerializerContext
{
}
