using Avalonia.Controls;
using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Pulsar2.Plugins.List;

class LocalPluginList
{
    private readonly ConcurrentDictionary<string, LocalPluginData> localPlugins = [];

    public IEnumerable<LocalPluginData> LocalPlugins => localPlugins.Values;

    public event Action<LocalPluginData> OnPluginAdded;

    public LocalPluginList()
    {
    }


    public async Task AddPluginsAsync(CancellationToken cancelToken = default)
    {
        await AddPluginsAsync(Path.Combine(FileUtilities.GameAppData, "Plugins"), cancelToken: cancelToken);
        await AddPluginsAsync(Path.Combine(FileUtilities.AppData, "plugins", "local"), cancelToken: cancelToken);
    }

    public async Task AddPluginsAsync(string folder, SearchOption searchOption = SearchOption.AllDirectories, CancellationToken cancelToken = default)
    {
        if (!Directory.Exists(folder))
            return;

        await AddPluginsAsync(Directory.EnumerateFiles(folder, "*.dll", searchOption), cancelToken);
    }

    public async Task AddPluginsAsync(IEnumerable<string> dllFiles, CancellationToken cancelToken = default)
    {
        await Task.Run(() => AddPlugins(dllFiles, cancelToken), cancelToken);
    }

    private void AddPlugins(IEnumerable<string> dllFiles, CancellationToken cancelToken)
    {
        foreach (string dll in dllFiles)
        {
            cancelToken.ThrowIfCancellationRequested();

            if (!localPlugins.ContainsKey(dll) && TryCreateDllPlugin(dll, out LocalPluginData local, cancelToken))
            {
                OnPluginAdded?.Invoke(local);
                localPlugins[local.FullPath] = local;
            }
        }
    }


    public static bool TryCreateDllPlugin(string dllFile, out LocalPluginData result, CancellationToken cancelToken = default)
    {
        result = null;

        if (!File.Exists(dllFile))
            return false;

        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(dllFile);
        }
        catch
        {
            return false;
        }

        if (!ContainsIPlugin(dllFile, cancelToken))
            return false;

        result = new LocalPluginData(dllFile, assemblyName);
        return true;
    }

    private static bool ContainsIPlugin(string dllFile, CancellationToken cancelToken = default)
    {
        var types = AssemblyDefinition
             .ReadAssembly(dllFile)
             .MainModule
             .Types;

        cancelToken.ThrowIfCancellationRequested();

        foreach (TypeDefinition t in types)
        {
            cancelToken.ThrowIfCancellationRequested();

            if (ContainsIPlugin(t))
                return true;
        }

        return false;
    }

    private static bool ContainsIPlugin(TypeDefinition type)
    {
        return !type.IsAbstract &&
            (type.Interfaces.Any(t => t.InterfaceType.FullName == "Keen.Game2.Game.Plugins.IPlugin")
            || (type.BaseType is TypeDefinition baseTypeDef && ContainsIPlugin(baseTypeDef)));
    }

    internal void Clear()
    {
        localPlugins.Clear();
    }

}
