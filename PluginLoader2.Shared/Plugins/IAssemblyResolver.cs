using System;
using System.IO;

namespace PluginLoader2.Plugins;

public interface IAssemblyResolver
{
    event Action<string> AssemblyResolved;

    void AddAllowedAssemblyFile(string assemblyFile);
    void AddAllowedAssemblyName(string assemblyName);
    void AddSourceFolder(string folder, SearchOption fileSearch = SearchOption.TopDirectoryOnly);
}