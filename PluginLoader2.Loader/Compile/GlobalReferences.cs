using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PluginLoader2.Loader.Compile;

static class GlobalReferences
{
    private static Dictionary<AssemblyKey, MetadataReference> allReferences = new Dictionary<AssemblyKey, MetadataReference>();

    public static void GenerateAssemblyList()
    {
        if (allReferences.Count > 0)
            return;

        Stack<Assembly> loadedAssemblies = new(AppDomain.CurrentDomain.GetAssemblies().Where(IsValidReference));

        StringBuilder sb = new StringBuilder();

        sb.AppendLine();
        string line = "===================================";
        sb.AppendLine(line);
        sb.AppendLine("Assembly References");
        sb.AppendLine(line);

        try
        {
            foreach (Assembly a in loadedAssemblies)
            {
                AddAssemblyReference(a);
                sb.AppendLine(a.FullName);
            }

            sb.AppendLine(line);
            while (loadedAssemblies.Count > 0)
            {
                Assembly a = loadedAssemblies.Pop();

                foreach (AssemblyName name in a.GetReferencedAssemblies())
                {
                    if (!ContainsReference(name) && TryLoadAssembly(name, out Assembly aRef) && IsValidReference(aRef))
                    {
                        AddAssemblyReference(aRef);
                        sb.AppendLine(name.FullName);
                        loadedAssemblies.Push(aRef);
                    }
                }
            }
            sb.AppendLine(line);
        }
        catch (Exception e)
        {
            sb.Append("Error: ").Append(e).AppendLine();
            Log.Error(sb.ToString());
        }

        Log.Info(sb.ToString());
    }

    private static bool ContainsReference(AssemblyName name)
    {
        return allReferences.ContainsKey(new AssemblyKey(name));
    }

    private static bool TryLoadAssembly(AssemblyName name, out Assembly aRef)
    {
        try
        {
            aRef = Assembly.Load(name);
            return true;
        }
        catch (IOException)
        {
            aRef = null;
            return false;
        }
    }

    private static void AddAssemblyReference(Assembly a)
    {
        AssemblyKey key = new AssemblyKey(a.GetName());
        if (!allReferences.ContainsKey(key))
            allReferences.Add(key, MetadataReference.CreateFromFile(a.Location));
    }

    public static IEnumerable<MetadataReference> GetReferences()
    {
        return allReferences.Values;
    }

    private static bool IsValidReference(Assembly a)
    {
        return !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location);
    }

    private class AssemblyKey : IEquatable<AssemblyKey>
    {
        public string Name { get; }
        public Version Version { get; }

        public AssemblyKey(AssemblyName name)
        {
            Name = name.Name;
            Version = name.Version;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AssemblyKey);
        }

        public bool Equals(AssemblyKey other)
        {
            return other is not null &&
                   Name == other.Name &&
                   EqualityComparer<Version>.Default.Equals(Version, other.Version);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Version);
        }

        public static bool operator ==(AssemblyKey left, AssemblyKey right)
        {
            return EqualityComparer<AssemblyKey>.Default.Equals(left, right);
        }

        public static bool operator !=(AssemblyKey left, AssemblyKey right)
        {
            return !(left == right);
        }
    }
}
