using Keen.Game2.Game.Plugins;
using System.Reflection;
using System;
using System.IO;
using System.Collections.Generic;
using Pulsar2.Loader;

namespace Pulsar2.Init
{
    /// <summary>
    /// This assembly is responsible only for loading the references and starting the full assembly
    /// </summary>
    public class PulsarInit : IPlugin
    {
        private static readonly Dictionary<string, string> assemblies = new Dictionary<string, string>();
        private Pulsar pluginLoader;

        public PulsarInit(PluginHost host)
        {
            Init();
            pluginLoader = new Pulsar(host);
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
            string name = targetAssembly.Name;
            if (name.StartsWith("Avalonia.Build.Tasks."))
                name = "Avalonia.Build.Tasks";
            if (assemblies.TryGetValue(name, out string targetPath) && File.Exists(targetPath))
                return Assembly.LoadFile(targetPath);
            return null;
        }
    }
}
