using Keen.Game2.Game.Plugins;
using System.Reflection;
using System;
using System.IO;
using System.Collections.Generic;
using PluginLoader2.Loader;

namespace PluginLoader2.Init
{
    /// <summary>
    /// This assembly is responsible only for loading the references and starting the full assembly
    /// </summary>
    public class PluginLoaderInit : IPlugin
    {
        private static readonly Dictionary<string, string> assemblies = new Dictionary<string, string>();
        private PluginLoader pluginLoader;

        public PluginLoaderInit(PluginHost host)
        {
            Init();
            pluginLoader = new PluginLoader(host);
        }

        private static void Init()
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (string name in Directory.EnumerateFiles(folder, "*.dll"))
                assemblies.TryAdd(Path.GetFileNameWithoutExtension(name), name);
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            AssemblyName targetAssembly = new AssemblyName(args.Name);
            if (assemblies.TryGetValue(targetAssembly.Name, out string targetPath) && File.Exists(targetPath))
                return Assembly.LoadFile(targetPath);
            return null;
        }
    }
}
