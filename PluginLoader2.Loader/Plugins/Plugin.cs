using Keen.Game2.Game.Plugins;
using Keen.VRage.Library.Reflection;
using Keen.VRage.Library.Utils;
using PluginLoader2.Plugins;
using System;
using System.Reflection;

namespace PluginLoader2.Loader.Plugins
{
    internal class Plugin
    {
        private ICustomPlugin data;
        private IPlugin plugin;

        public Plugin (ICustomPlugin data)
        {
            this.data = data;
        }

        public void Instantiate(PluginHost host)
        {
            Assembly assembly = data.GetAssembly(new AssemblyResolver());
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
            {
                if (typeof(IPlugin)!.IsAssignableFrom(type) && !type.IsAbstract)
                {
                    Singleton<MetadataManager>.Instance.PushContext(assembly);
                    Instantiate(type, host);
                }
            }
        }

        public void Instantiate(Type pluginType, PluginHost host)
        {
            if (pluginType.GetConstructor([typeof(PluginHost)]) != null)
                plugin = (IPlugin)Activator.CreateInstance(pluginType, this);
            else
                plugin = (IPlugin)Activator.CreateInstance(pluginType);
        }
    }
}
