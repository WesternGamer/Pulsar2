using System.Reflection;

namespace PluginLoader2.Plugins;

interface ICustomPlugin
{
    Assembly GetAssembly(IAssemblyResolver resolver);
}