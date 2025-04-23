using System.Reflection;

namespace PluginLoader2.Loader.Plugins;

interface IPluginInstance
{
    Assembly Load(); 
}
