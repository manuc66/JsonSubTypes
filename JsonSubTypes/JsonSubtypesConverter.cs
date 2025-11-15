using System;
using System.Linq;

namespace JsonSubTypes
{
    //  MIT License
    //  
    //  Copyright (c) 2017 Emmanuel Counasse
    //  
    //  Permission is hereby granted, free of charge, to any person obtaining a copy
    //  of this software and associated documentation files (the "Software"), to deal
    //  in the Software without restriction, including without limitation the rights
    //  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    //  copies of the Software, and to permit persons to whom the Software is
    //  furnished to do so, subject to the following conditions:
    //  
    //  The above copyright notice and this permission notice shall be included in all
    //  copies or substantial portions of the Software.
    //  
    //  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    //  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    //  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    //  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    //  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    //  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    //  SOFTWARE.
    public class JsonSubtypesConverter : JsonSubtypes
    {
        private readonly Type _baseType;
        private readonly Type _fallbackType;

        internal JsonSubtypesConverter(Type baseType, Type fallbackType) : base()
        {
            _baseType = baseType;
            _fallbackType = fallbackType;
        }

        internal JsonSubtypesConverter(Type baseType, string jsonDiscriminatorPropertyName, Type fallbackType) : base(jsonDiscriminatorPropertyName)
        {
            _baseType = baseType;
            _fallbackType = fallbackType;
        }

        internal override Type GetFallbackSubType(Type type)
        {
            return _fallbackType;
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType == _baseType || ToTypeInfo(_baseType).IsAssignableFrom(ToTypeInfo(objectType)))
            {
                return true;
            }

            return InheritsOrImplementsGeneric(objectType, _baseType);
        }

        protected static bool InheritsOrImplementsGeneric(Type objectType, Type baseType)
        {
            // Cas générique ouvert : _baseType = Base<> et objectType = Base<int>
            if (ToTypeInfo(baseType).IsGenericTypeDefinition && ToTypeInfo(objectType).IsGenericType)
            {
                // Comparer la définition générique
                if (objectType.GetGenericTypeDefinition() == baseType)
                {
                    return true;
                }

                // Optionnel : remonter la hiérarchie des bases
                var current = ToTypeInfo(objectType).BaseType;
                while (current != null)
                {
                    if (ToTypeInfo(current).IsGenericType && current.GetGenericTypeDefinition() == baseType)
                    {
                        return true;
                    }

                    current = ToTypeInfo(current).BaseType;
                }

                // Optionnel : interfaces génériques
                if (GetImplementedInterfaces(objectType)
                    .Any(iface => ToTypeInfo(iface).IsGenericType && iface.GetGenericTypeDefinition() == baseType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
