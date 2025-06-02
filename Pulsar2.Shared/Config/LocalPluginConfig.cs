using Pulsar2.Plugins;

namespace Pulsar2.Config;

public class LocalPluginConfig
{
    public LocalPluginConfig()
    {

    }

    public LocalPluginConfig(string fullPath)
    {
        FullPath = fullPath;
    }

    public LocalPluginConfig(string fullPath, string name) : this(fullPath)
    {
        Name = name;
    }

    public LocalPluginConfig(LocalPluginData data) : this(data.FullPath, data.Name)
    {

    }
        

    public string FullPath { get; set; }
    public string Name { get; set; }

}
