using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JsonSubTypes.Text.Json
{
    public static class JsonSubTypesTypeResolution
    {
        private static readonly ConcurrentDictionary<Assembly, byte> Assemblies =
            new ConcurrentDictionary<Assembly, byte>();

        public static void AddAssembly(Assembly assembly)
        {
            Assemblies.TryAdd(assembly, 0);
        }

        public static void RemoveAssembly(Assembly assembly)
        {
            Assemblies.TryRemove(assembly, out _);
        }

        public static void ClearAssemblies()
        {
            Assemblies.Clear();
        }

        public static IReadOnlyCollection<Assembly> SearchAssemblies => Assemblies.Keys.ToArray();

        internal static IEnumerable<Assembly> GetSearchAssemblies(Assembly parentAssembly)
        {
            yield return parentAssembly;
            foreach (Assembly assembly in Assemblies.Keys)
            {
                if (assembly != parentAssembly)
                {
                    yield return assembly;
                }
            }
        }
    }
}
