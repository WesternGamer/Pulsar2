using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace PluginLoader2.Plugins;

public class LocalPluginData
{
    public string FullPath { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string FileName { get; set; }

    public LocalPluginData()
    {

    }
    public LocalPluginData(string dllFile)
    {
        FileName = Path.GetFileName(dllFile);
        FullPath = dllFile;
    }
    public LocalPluginData(string dllFile, AssemblyName assemblyName) : this(dllFile)
    {
        Name = assemblyName.Name;
        Version = assemblyName.Version?.ToString();
    }

}