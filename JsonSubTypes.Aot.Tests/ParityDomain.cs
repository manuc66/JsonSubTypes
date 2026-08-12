#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

namespace JsonSubTypes.Aot.Tests
{
    // Domain types shared by the runtime-converter and generated-converter parity fixtures.
    // The marker + KnownSubType attributes drive the generator; the runtime fixture mirrors the
    // same registration through the builders.

    public class Root
    {
        public Base? Content { get; set; }
        public List<Base>? ContentList { get; set; }

        protected bool Equals(Root other)
        {
            if (Equals(Content, other.Content))
            {
                return ContentList == null || other.ContentList == null
                    ? ReferenceEquals(ContentList, other.ContentList)
                    : ContentList.SequenceEqual(other.ContentList);
            }
            return false;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Root)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Content != null ? Content.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (ContentList != null
                               ? ContentList.Aggregate(0, (x, y) => x.GetHashCode() ^ y.GetHashCode())
                               : 0);
                return hashCode;
            }
        }
    }

    [JsonSubTypesAotConverter("@type")]
    [KnownSubType(typeof(SubB), "SubB")]
    [KnownSubType(typeof(SubC), "SubC")]
    public class Base
    {
        [JsonPropertyName("@type")]
        public virtual string Type => "";

        [JsonPropertyName("4-you")]
        public int _4You { get; set; }

        protected bool Equals(Base other) => string.Equals(Type, other.Type) && _4You == other._4You;
        public override bool Equals(object? obj) => obj is Base b && Equals(b);
        public override int GetHashCode() => (Type.GetHashCode() * 397) ^ _4You;
    }

    public class SubB : Base
    {
        [JsonPropertyName("@type")]
        public override string Type => "SubB";

        public int Index { get; set; }

        protected bool Equals(SubB other) => base.Equals(other) && Index == other.Index;
        public override bool Equals(object? obj) => obj is SubB b && Equals(b);
        public override int GetHashCode() => (base.GetHashCode() * 397) ^ Index;
    }

    public class SubC : Base
    {
        [JsonPropertyName("@type")]
        public override string Type => "SubC";

        public string? Name { get; set; }

        protected bool Equals(SubC other) => base.Equals(other) && string.Equals(Name, other.Name);
        public override bool Equals(object? obj) => obj is SubC c && Equals(c);
        public override int GetHashCode() => (base.GetHashCode() * 397) ^ (Name?.GetHashCode() ?? 0);
    }

    [JsonSubTypesAotConverter(nameof(MainClass.Discriminator))]
    [KnownSubType(typeof(SomeSubtype), "some")]
    public abstract class MainClass
    {
        public string Discriminator { get; set; } = "";
    }

    public class SomeSubtype : MainClass
    {
    }

    [JsonSubTypesAotConverter("Sound")]
    [KnownSubType(typeof(PDog), "Bark")]
    [KnownSubType(typeof(PCat), "Meow")]
    public interface IAnimal
    {
    }

    public class PDog : IAnimal
    {
        public string? Breed { get; set; }
    }

    public class PCat : IAnimal
    {
        public bool Declawed { get; set; }
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

    [JsonSubTypesAotConverter("Kind")]
    [KnownSubType(typeof(PDog), "Dog")]
    public class PAnimal
    {
        public string? Name { get; set; }

        [JsonIgnore]
        public string? Secret { get; set; }
    }

    [JsonSubTypesAotConverter]
    [KnownSubTypeWithProperty(typeof(PEmployee), "JobTitle")]
    [KnownSubTypeWithProperty(typeof(PEmployee), "Department")]
    [KnownSubTypeWithProperty(typeof(PArtist), "Skill")]
    public class MultiPropBase
    {
        public string? FirstName { get; set; }
    }

    public class PEmployee : MultiPropBase
    {
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
    }

    public class PArtist : MultiPropBase
    {
        public string? Skill { get; set; }
    }

    [JsonSubTypesAotConverter("Kind")]
    [KnownSubType(typeof(ParameterizedDerived), "Derived")]
    public class ParameterizedBase
    {
        public ParameterizedBase(string name)
        {
        }
    }

    public class ParameterizedDerived : ParameterizedBase
    {
        public ParameterizedDerived() : base("x")
        {
        }
    }

    [JsonSubTypesAotConverter("msgType")]
    [KnownSubType(typeof(Foo), 1)]
    public abstract class DtoBase
    {
    }

    public class Foo : DtoBase
    {
        public int MsgType { get; set; }
    }
}
