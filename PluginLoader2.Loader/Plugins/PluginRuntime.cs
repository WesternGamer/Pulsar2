using Keen.Game2.Game.Plugins;
using Keen.VRage.Library.Reflection;
using Keen.VRage.Library.Utils;
using PluginLoader2.Plugins;
using System;
using System.Net.Http;
using System.Reflection;

namespace PluginLoader2.Loader.Plugins
{
    internal class PluginRuntime
    {
        private IPluginInstance instance;
        private IPlugin plugin;

        public PluginRuntime (IPluginInstance instance)
        {
            this.instance = instance;
        }

        public void Instantiate(PluginHost host)
        {
            Assembly assembly = instance.Load();
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
