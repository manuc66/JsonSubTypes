using System;

namespace JsonSubTypes.Text.Json
{
    public class UnresolvedSubtypeInfo
    {
        public Type ParentType { get; }
        public string? DiscriminatorPropertyName { get; }
        public object? DiscriminatorValue { get; }
        public bool HasDiscriminator { get; }
        public Type? FallbackSubtype { get; }

        public UnresolvedSubtypeInfo(Type parentType, string? discriminatorPropertyName, object? discriminatorValue,
            bool hasDiscriminator, Type? fallbackSubtype)
        {
            ParentType = parentType;
            DiscriminatorPropertyName = discriminatorPropertyName;
            DiscriminatorValue = discriminatorValue;
            HasDiscriminator = hasDiscriminator;
            FallbackSubtype = fallbackSubtype;
        }
    }
}
