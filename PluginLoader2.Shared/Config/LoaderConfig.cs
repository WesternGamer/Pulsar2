using System.Collections.Generic;
using System.Xml.Serialization;

namespace PluginLoader2.Config;

public class LoaderConfig : ConfigFile
{
    public HashSet<string> LocalPlugins { get; set; } = [];
    public HashSet<string> GitHubPlugins { get; set; } = [];

    protected override void Init()
    {
        if (LocalPlugins == null)
            LocalPlugins = [];
    }

}