#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Aot.Generated;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    [TestFixture]
    public class GeneratedEnumDiscriminatorTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.EAnimal } };
        }

        [Test]
        public void Serialize_WithStringEnumConverter_WritesEnumName()
        {
            var options = Options();
            options.Converters.Add(new JsonStringEnumConverter());

            string json = JsonSerializer.Serialize<EAnimal>(new ECat { Age = 3, Lives = 9 }, options);

            StringAssert.StartsWith("{\"kind\":\"Cat\"", json);
        }

        [Test]
        public void Serialize_WithoutConverter_WritesEnumNumber()
        {
            string json = JsonSerializer.Serialize<EAnimal>(new ECat { Age = 3, Lives = 9 }, Options());

            StringAssert.StartsWith("{\"kind\":0", json);
        }

        [Test]
        public void Deserialize_StringEnumName_ReturnsSubtype()
        {
            var options = Options();
            options.Converters.Add(new JsonStringEnumConverter());

            var result = JsonSerializer.Deserialize<EAnimal>("{\"kind\":\"Dog\",\"CanHunt\":true,\"Age\":4}", options);

            Assert.IsInstanceOf<EDog>(result);
        }

        [Test]
        public void Deserialize_EnumNumber_ReturnsSubtype()
        {
            var result = JsonSerializer.Deserialize<EAnimal>("{\"kind\":1,\"CanHunt\":true,\"Age\":4}", Options());

            Assert.IsInstanceOf<EDog>(result);
        }
    }

    [TestFixture]
    public class GeneratedNullDiscriminatorTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.NullAnimal } };
        }

        [Test]
        public void Serialize_BaseWithNullDiscriminator_WritesNull()
        {
            string json = JsonSerializer.Serialize<NullAnimal>(new NullAnimal { Age = 1 }, Options());

            Assert.AreEqual("{\"type\":null,\"Age\":1}", json);
        }

        [Test]
        public void Deserialize_NullDiscriminator_ReturnsBase()
        {
            var result = JsonSerializer.Deserialize<NullAnimal>("{\"type\":null,\"Age\":1}", Options());

            Assert.IsInstanceOf<NullAnimal>(result);
        }
    }

    [TestFixture]
    public class GeneratedDiscriminatorLastTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.DAnimal } };
        }

        [Test]
        public void Serialize_WritesDiscriminatorLast()
        {
            string json = JsonSerializer.Serialize<DAnimal>(new DCat { Age = 1, Lives = 9 }, Options());

            Assert.AreEqual("{\"Lives\":9,\"Age\":1,\"type\":\"cat\"}", json);
        }

        [Test]
        public void Deserialize_StillWorks()
        {
            var result = JsonSerializer.Deserialize<DAnimal>("{\"type\":\"cat\",\"Lives\":9,\"Age\":1}", Options());

            Assert.IsInstanceOf<DCat>(result);
        }
    }

    [TestFixture]
    public class GeneratedDiscriminatorNameMatchingTests
    {
        [Test]
        public void CaseInsensitive_MatchesDiscriminatorName()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { JsonSubTypesAotConverters.CIAnimal }
            };

            var result = JsonSerializer.Deserialize<CIAnimal>("{\"type\":\"cat\",\"Lives\":9,\"Age\":1}", options);

            Assert.IsInstanceOf<CICat>(result);
        }

        [Test]
        public void NamingPolicy_AppliedToDiscriminatorName_OnWrite()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { JsonSubTypesAotConverters.NPAnimal }
            };

            string json = JsonSerializer.Serialize<NPAnimal>(new NPCat { Age = 1, Lives = 9 }, options);

            StringAssert.StartsWith("{\"kind\":\"cat\"", json);
        }

        [Test]
        public void NamingPolicy_AppliedToDiscriminatorName_OnRead()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { JsonSubTypesAotConverters.NPAnimal }
            };

            var result = JsonSerializer.Deserialize<NPAnimal>("{\"kind\":\"cat\",\"lives\":9,\"age\":1}", options);

            Assert.IsInstanceOf<NPCat>(result);
        }
    }

    [TestFixture]
    public class GeneratedDottedDiscriminatorTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.DPAnimal } };
        }

        [Test]
        public void Deserialize_NestedDiscriminator_ReturnsSubtype()
        {
            var result = JsonSerializer.Deserialize<DPAnimal>("{\"nested\":{\"type\":\"cat\"},\"Lives\":9,\"Age\":1}", Options());

            Assert.IsInstanceOf<DPCat>(result);
        }

        [Test]
        public void RoundTrip_FlatDottedKey_WritesAndReads()
        {
            var options = Options();
            string json = JsonSerializer.Serialize<DPAnimal>(new DPCat { Age = 1, Lives = 9 }, options);

            StringAssert.StartsWith("{\"nested.type\":\"cat\"", json);

            var back = JsonSerializer.Deserialize<DPAnimal>(json, options);
            Assert.IsInstanceOf<DPCat>(back);
        }
    }

    [TestFixture]
    public class GeneratedDirectAndNestedSubtypeTests
    {
        // Pins the routing when a subtype is registered both directly on the base and
        // reachable through an intermediate base. The generator must prefer the direct
        // registration and not emit a duplicate nested chain (see BuildNestedChains).
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.DNRoot } };
        }

        [Test]
        public void Serialize_DirectRegistration_IsPreferredOverNestedChain()
        {
            string json = JsonSerializer.Serialize<DNRoot>(new DNLeaf { Age = 1, Mark = 2 }, Options());

            // the direct registration ("leaf" on Root) wins; no nested chain is emitted
            Assert.AreEqual("{\"kind\":\"leaf\",\"Mark\":2,\"Age\":1}", json);
        }

        [Test]
        public void Deserialize_DirectDiscriminator_ReturnsSubtype()
        {
            var result = JsonSerializer.Deserialize<DNRoot>("{\"kind\":\"leaf\",\"Age\":1}", Options());

            Assert.IsInstanceOf<DNLeaf>(result);
        }

        [Test]
        public void Deserialize_NestedDiscriminator_StillWorksForIntermediate()
        {
            var result = JsonSerializer.Deserialize<DNRoot>("{\"kind\":\"mid\",\"Age\":1}", Options());

            Assert.IsInstanceOf<DNMid>(result);
        }
    }

    // ---- domain types ----

    public enum EAnimalKind
    {
        Cat,
        Dog
    }

    [JsonSubTypesAotConverter("kind")]
    [KnownSubType(typeof(ECat), EAnimalKind.Cat)]
    [KnownSubType(typeof(EDog), EAnimalKind.Dog)]
    public class EAnimal
    {
        public int Age { get; set; }
    }

    public class ECat : EAnimal
    {
        public int Lives { get; set; }
    }

    public class EDog : EAnimal
    {
        public bool CanHunt { get; set; }
    }

    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(NullAnimal), null)]
    [KnownSubType(typeof(NullCat), "cat")]
    public class NullAnimal
    {
        public int Age { get; set; }
    }

    public class NullCat : NullAnimal
    {
        public int Lives { get; set; }
    }

    [JsonSubTypesAotConverter("type", AddDiscriminatorFirst = false)]
    [KnownSubType(typeof(DCat), "cat")]
    public class DAnimal
    {
        public int Age { get; set; }
    }

    public class DCat : DAnimal
    {
        public int Lives { get; set; }
    }

    [JsonSubTypesAotConverter("Type")]
    [KnownSubType(typeof(CICat), "cat")]
    public class CIAnimal
    {
        public int Age { get; set; }
    }

    public class CICat : CIAnimal
    {
        public int Lives { get; set; }
    }

    [JsonSubTypesAotConverter("Kind")]
    [KnownSubType(typeof(NPCat), "cat")]
    public class NPAnimal
    {
        public int Age { get; set; }
    }

    public class NPCat : NPAnimal
    {
        public int Lives { get; set; }
    }

    [JsonSubTypesAotConverter("nested.type")]
    [KnownSubType(typeof(DPCat), "cat")]
    public class DPAnimal
    {
        public int Age { get; set; }
    }

    public class DPCat : DPAnimal
    {
        public int Lives { get; set; }
    }
}

[TestFixture]
public class GeneratedDynamicRegistrationTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.DynAnimal } };
    }

    [Test]
    public void Deserialize_StaticSubtype_Works()
    {
        var result = JsonSerializer.Deserialize<DynAnimal>("{\"type\":\"cat\",\"Lives\":9,\"Age\":4}", Options());

        Assert.IsInstanceOf<DynCat>(result);
    }

    [Test]
    public void Deserialize_DynamicSubtype_RegisteredAtRuntime()
    {
        JsonSubTypesAotConverters.DynAnimal.DynamicSubtypes["dog"] = typeof(DynDog);

        try
        {
            var result = JsonSerializer.Deserialize<DynAnimal>("{\"type\":\"dog\",\"CanHunt\":true,\"Age\":4}", Options());
            Assert.IsInstanceOf<DynDog>(result);
        }
        finally
        {
            JsonSubTypesAotConverters.DynAnimal.DynamicSubtypes.TryRemove("dog", out _);
        }
    }

    [Test]
    public void Serialize_DynamicSubtype_WritesDiscriminator()
    {
        JsonSubTypesAotConverters.DynAnimal.RegisterDynamicSubtype("dog", typeof(DynDog));

        try
        {
            string json = JsonSerializer.Serialize<DynAnimal>(new DynDog { CanHunt = true, Age = 4 }, Options());
            Assert.AreEqual("{\"type\":\"dog\",\"CanHunt\":true,\"Age\":4}", json);
        }
        finally
        {
            JsonSubTypesAotConverters.DynAnimal.DynamicSubtypes.TryRemove("dog", out _);
        }
    }

    [Test]
    public void Deserialize_CustomTypeNameResolver_ResolvesArbitraryName()
    {
        JsonSubTypesAotConverters.DynAnimal.CustomTypeNameResolver = name =>
            name as string == "custom-dog" ? typeof(DynDog) : null;

        try
        {
            var result = JsonSerializer.Deserialize<DynAnimal>("{\"type\":\"custom-dog\",\"CanHunt\":true,\"Age\":4}", Options());
            Assert.IsInstanceOf<DynDog>(result);
        }
        finally
        {
            JsonSubTypesAotConverters.DynAnimal.CustomTypeNameResolver = null;
        }
    }

    [Test]
    public void Deserialize_UnknownDiscriminator_StillFallsBack()
    {
        var result = JsonSerializer.Deserialize<DynAnimal>("{\"type\":\"fish\",\"Age\":4}", Options());

        Assert.IsInstanceOf<DynAnimal>(result);
    }
}

[JsonSubTypesAotConverter("type")]
[KnownSubType(typeof(DynCat), "cat")]
public class DynAnimal
{
    public int Age { get; set; }
}

public class DynCat : DynAnimal
{
    public int Lives { get; set; }
}

public class DynDog : DynAnimal
{
    public bool CanHunt { get; set; }
}

[TestFixture]
public class GeneratedDuplicateDiscriminatorTests
{
    private static JsonSerializerOptions Options()
    {
        return new JsonSerializerOptions { Converters = { JsonSubTypesAotConverters.DupAnimal } };
    }

    [Test]
    public void Serialize_UsesLastRegisteredDiscriminator()
    {
        string json = JsonSerializer.Serialize<DupAnimal>(new DupCat { Age = 1, Lives = 9 }, Options());

        Assert.AreEqual("{\"type\":\"feline\",\"Lives\":9,\"Age\":1}", json);
    }

    [Test]
    public void Deserialize_AcceptsBothDiscriminators()
    {
        Assert.IsInstanceOf<DupCat>(JsonSerializer.Deserialize<DupAnimal>("{\"type\":\"cat\",\"Age\":1}", Options()));
        Assert.IsInstanceOf<DupCat>(JsonSerializer.Deserialize<DupAnimal>("{\"type\":\"feline\",\"Age\":1}", Options()));
    }
}

#pragma warning disable JSTAOT002 // intentional duplicate discriminators; the test pins the last-wins behavior
[JsonSubTypesAotConverter("type")]
[KnownSubType(typeof(DupCat), "cat")]
[KnownSubType(typeof(DupCat), "feline")]
public class DupAnimal
{
    public int Age { get; set; }
}
#pragma warning restore JSTAOT002

public class DupCat : DupAnimal
{
    public int Lives { get; set; }
}

[JsonSubTypesAotConverter("kind")]
[KnownSubType(typeof(DNMid), "mid")]
[KnownSubType(typeof(DNLeaf), "leaf")]
public class DNRoot
{
    public int Age { get; set; }
}

[JsonSubTypesAotConverter("kind")]
[KnownSubType(typeof(DNLeaf), "leaf")]
public class DNMid : DNRoot
{
}

public class DNLeaf : DNMid
{
    public int Mark { get; set; }
}
