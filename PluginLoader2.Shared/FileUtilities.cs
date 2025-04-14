using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PluginLoader2;

internal static class FileUtilities
{
    public static string AppData { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "PluginLoader2");

    public static string GameAppData { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "SpaceEngineers2");

}