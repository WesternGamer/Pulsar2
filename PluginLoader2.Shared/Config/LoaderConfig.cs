using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace PluginLoader2.Config;

public class LoaderConfig : ConfigFile
{
    public LocalPluginConfig[] LocalPluginList
    {
        get => LocalPlugins.Values.ToArray();
        set => LocalPlugins = value.ToDictionary(x => x.FullPath);
    }
    [XmlIgnore]
    public Dictionary<string, LocalPluginConfig> LocalPlugins { get; set; } = [];

    public GitHubPluginConfig[] GitHubPluginList 
    {
        get => GitHubPlugins.Values.ToArray();
        set => GitHubPlugins = value.ToDictionary(x => x.Id);
    }
    [XmlIgnore]
    public Dictionary<string, GitHubPluginConfig> GitHubPlugins { get; set; } = [];

    protected override void Init()
    {
        LocalPlugins ??= [];
        GitHubPlugins ??= [];
    }

}