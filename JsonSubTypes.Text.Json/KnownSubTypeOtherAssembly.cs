using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace JsonSubTypes.Text.Json;

/// <summary>
/// Declares another assembly to search, in addition to the base type's own assembly, when
/// resolving subtypes by name from the JSON discriminator for the decorated polymorphic base
/// type. The assembly is referenced by name so the base type does not need a compile-time
/// reference to it, which is what keeps the plugin pattern cycle-free: the plugin references the
/// base, the base merely names the plugin.
/// </summary>
/// <remarks>
/// The assignability guard still applies: only types assignable from the base type are
/// considered. This attribute replaces the global <c>JsonSubTypesTypeResolution.AddAssembly</c>
/// registry, which leaked state across serialization profiles; resolution is now declared on the
/// base type and cached per type.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class KnownSubTypeOtherAssembly(string assemblyName) : Attribute
{
    public string AssemblyName { get; } = assemblyName;
}

internal static class TypeResolution
{
    private static readonly ConcurrentDictionary<TypeInfo, Assembly[]> AssembliesByBaseType = new();

    public static Assembly[] GetSearchAssemblies(TypeInfo baseType)
    {
        return AssembliesByBaseType.GetOrAdd(baseType, static type =>
        {
            List<Assembly> assemblies = [type.Assembly];
            foreach (object attribute in type.GetCustomAttributes(false))
            {
                if (attribute is KnownSubTypeOtherAssembly otherAssembly)
                {
                    Assembly? assembly = FindAssembly(otherAssembly.AssemblyName);
                    if (assembly != null && !assemblies.Contains(assembly))
                    {
                        assemblies.Add(assembly);
                    }
                }
            }

            return [.. assemblies];
        });
    }

    private static Assembly? FindAssembly(string assemblyName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
            {
                return assembly;
            }
        }

        return null;
    }
}
