using System;

namespace JsonSubTypes
{
    /// <summary>
    /// Describes a JSON object whose subtype could not be resolved by the converter
    /// (unknown or missing discriminator value, or no matching property in
    /// property-presence mode).
    /// </summary>
    public class UnresolvedSubtypeInfo
    {
        /// <summary>Gets the type for which the subtype was resolved.</summary>
        public Type ParentType { get; }

        /// <summary>
        /// Gets the name of the discriminator property, or <c>null</c> when the converter
        /// resolves by property presence.
        /// </summary>
        public string DiscriminatorPropertyName { get; }

        /// <summary>
        /// Gets the discriminator value read from the JSON object, or <c>null</c> when the
        /// discriminator property is missing or holds a JSON <c>null</c>.
        /// </summary>
        public object DiscriminatorValue { get; }

        /// <summary>
        /// Gets a value indicating whether the discriminator property was present in the
        /// JSON object. Use this to distinguish a missing discriminator from an unknown
        /// value (<see cref="DiscriminatorValue" /> is <c>null</c> in both cases).
        /// </summary>
        public bool HasDiscriminator { get; }

        /// <summary>
        /// Gets the fallback subtype that will be used for this JSON object, or <c>null</c>
        /// when no fallback subtype is configured.
        /// </summary>
        public Type FallbackSubtype { get; }

        public UnresolvedSubtypeInfo(Type parentType, string discriminatorPropertyName, object discriminatorValue,
            bool hasDiscriminator, Type fallbackSubtype)
        {
            ParentType = parentType;
            DiscriminatorPropertyName = discriminatorPropertyName;
            DiscriminatorValue = discriminatorValue;
            HasDiscriminator = hasDiscriminator;
            FallbackSubtype = fallbackSubtype;
        }
    }
}
