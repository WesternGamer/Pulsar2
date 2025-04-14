using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PluginLoader2.Plugins;

internal class LocalPlugin : ICustomPlugin
{
    private readonly string dllFile;

    public string FullPath { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }


    public LocalPlugin(string dllFile)
    {
        this.dllFile = dllFile;
        FileName = Path.GetFileName(dllFile);
        FullPath = dllFile;
    }
    public LocalPlugin(string dllFile, AssemblyName assemblyName) : this(dllFile)
    {
        Name = assemblyName.Name;
        Version = assemblyName.Version?.ToString();
    }

    public Assembly GetAssembly(IAssemblyResolver resolver)
    {
        resolver.AddAllowedAssemblyFile(dllFile);
        resolver.AddSourceFolder(Path.GetDirectoryName(dllFile));

        return Assembly.LoadFile(dllFile);
    }
}