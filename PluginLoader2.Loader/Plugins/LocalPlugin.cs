using PluginLoader2.Plugins;
using System.IO;
using System.Reflection;

namespace PluginLoader2.Loader.Plugins;

class LocalPlugin : IPluginInstance
{
    private readonly string dllFile;
    private AssemblyResolver resolver;

    public LocalPlugin(LocalPluginData data)
    {
        dllFile = data.FullPath;
    }

    public Assembly Load()
    {
        resolver = new AssemblyResolver();
        resolver.AddAllowedAssemblyFile(dllFile);
        resolver.AddSourceFolder(Path.GetDirectoryName(dllFile));

        return Assembly.LoadFile(dllFile);
    }

}
