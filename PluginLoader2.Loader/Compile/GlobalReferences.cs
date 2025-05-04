using Basic.Reference.Assemblies;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Keen.Game2;

namespace PluginLoader2.Loader.Compile;

static class GlobalReferences
{
    private static Dictionary<string, MetadataReference> systemReferences = [];
    private static Dictionary<AssemblyKey, MetadataReference> allReferences = [];
    private static readonly HashSet<string> blockedAssemblies = ["VRage.Library.Generator"];

    public static void GenerateAssemblyList()
    {
        if (allReferences.Count > 0)
            return;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine();
        string line = "===================================";

        sb.AppendLine(line);
        sb.AppendLine("Reference Assemblies");
        sb.AppendLine(line);

        systemReferences = ReferenceAssemblies.Net80.ToDictionary(x => Path.GetFileNameWithoutExtension(x.FilePath), x => (MetadataReference)x);
        foreach (string name in systemReferences.Keys)
            sb.AppendLine(name);

        Dictionary<AssemblyKey, Assembly> loadedAssemblies = [];

        sb.AppendLine(line);
        sb.AppendLine("Trusted Platform Assemblies");
        sb.AppendLine(line);

        foreach (string trustedAssemblyFile in GetBinAssemblies())
        {
            if(AssemblyKey.TryGetFromFile(trustedAssemblyFile, out AssemblyKey trustedAssemblyName)
                && !systemReferences.ContainsKey(trustedAssemblyName.Name) 
                && IsValidReference(trustedAssemblyName.Name)
                && TryLoadAssembly(trustedAssemblyName.FullName, out Assembly trustedAssembly)
                && loadedAssemblies.TryAdd(trustedAssemblyName, trustedAssembly))
            {
                allReferences.TryAdd(trustedAssemblyName, MetadataReference.CreateFromFile(trustedAssemblyFile));
                sb.AppendLine(trustedAssembly.FullName);
            }
        }

        sb.AppendLine(line);
        sb.AppendLine("Loaded Assemblies");
        sb.AppendLine(line);

        foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies().Where(IsValidReference).Concat(ManualReferences()))
        {
            AssemblyKey key = new AssemblyKey(a);
            if(!systemReferences.ContainsKey(key.Name) && loadedAssemblies.TryAdd(key, a))
            {
                allReferences.TryAdd(key, MetadataReference.CreateFromFile(a.Location));
                sb.AppendLine(a.FullName);
            }
        }

        sb.AppendLine(line);
        sb.AppendLine("Dependency Assemblies");
        sb.AppendLine(line);

        try
        {

            Stack<Assembly> workingAssemblies = new(loadedAssemblies.Values);
            while (workingAssemblies.Count > 0)
            {
                Assembly a = workingAssemblies.Pop();

                foreach (AssemblyName refName in a.GetReferencedAssemblies())
                {
                    AssemblyKey key = new(refName);
                    if (!allReferences.ContainsKey(key)
                        && !systemReferences.ContainsKey(key.Name)
                        && TryLoadAssembly(refName, out Assembly aRef) 
                        && IsValidReference(aRef) 
                        && allReferences.TryAdd(new AssemblyKey(aRef), MetadataReference.CreateFromFile(aRef.Location)))
                    {
                        sb.AppendLine(aRef.FullName);
                        workingAssemblies.Push(aRef);
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
#if DEBUG
        Log.Info(sb.ToString());
#endif
    }

    private static IEnumerable<string> GetBinAssemblies()
    {
        string baseGame = Path.GetDirectoryName(Path.GetFullPath(typeof(GameAppComponentObjectBuilder).Assembly.Location));
        string data = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (data == null)
            return [];
        return data.Split(';').Where(x => x.StartsWith(baseGame));
    }

    private static IEnumerable<Assembly> ManualReferences()
    {
        yield return typeof(Harmony).Assembly;
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

    public static IEnumerable<MetadataReference> GetReferences()
    {
        return systemReferences.Values.Concat(allReferences.Values);
    }

    private static bool IsValidReference(Assembly a)
    {
        return !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location) && IsValidReference(a.GetName().Name);
    }

    private static bool IsValidReference(string name)
    {
        return !systemReferences.ContainsKey(name) && !name.StartsWith("System.Private") && !blockedAssemblies.Contains(name);
    }

    private class AssemblyKey : IEquatable<AssemblyKey>
    {
        public string Name { get; }
        public int Major => Version.Major;
        public int Minor => Version.Minor;
        public int Build => Version.Build;
        public int Revision => Version.Revision;
        public Version Version { get; }
        public AssemblyName FullName { get; }

        public AssemblyKey(AssemblyName name)
        {
            Name = name.Name;
            Version = name.Version;
            FullName = name;
        }

        public AssemblyKey(Assembly a) : this(a.GetName())
        {
        }

        public override string ToString()
        {
            return $"{Name} - {Version}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AssemblyKey);
        }

        public bool Equals(AssemblyKey other)
        {
            return other is not null &&
                   Name == other.Name &&
                   Major == other.Major &&
                   Minor == other.Minor &&
                   Build == other.Build &&
                   Revision == other.Revision;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Major, Minor, Build, Revision);
        }

        public static bool operator ==(AssemblyKey left, AssemblyKey right)
        {
            return EqualityComparer<AssemblyKey>.Default.Equals(left, right);
        }

        public static bool operator !=(AssemblyKey left, AssemblyKey right)
        {
            return !(left == right);
        }

        public static bool TryGetFromFile(string fileName, out AssemblyKey result)
        {
            try
            {
                result = new AssemblyKey(AssemblyName.GetAssemblyName(fileName));
                return true;
            }
            catch (Exception e)
            {
                result = null;
                return false;
            }
        }
    }
}
