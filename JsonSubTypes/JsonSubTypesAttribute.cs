using System;
using System.Collections.Generic;

namespace JsonSubTypes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JsonSubTypesAttribute : Attribute
    {
        public string TypeProperty { get; }
        public Dictionary<Type, object> Mapping { get; }

        public JsonSubTypesAttribute(string typeProperty, Dictionary<Type, object> mapping)
        {
            TypeProperty = typeProperty;
            Mapping = mapping;
        }
    }
}