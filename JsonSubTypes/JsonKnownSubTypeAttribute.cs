using System;

namespace JsonSubTypes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class JsonKnownSubTypeAttribute : Attribute
    {
        public Type SubType { get; }
        public object AssociatedValue { get; }

        public JsonKnownSubTypeAttribute(Type subType, object associatedValue)
        {
            SubType = subType;
            AssociatedValue = associatedValue;
        }
    }
}