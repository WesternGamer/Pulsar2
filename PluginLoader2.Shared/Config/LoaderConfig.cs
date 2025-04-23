using System.Collections.Generic;

namespace PluginLoader2.Config;

public class LoaderConfig : ConfigFile
{
    public HashSet<string> LocalPlugins { get; set; } = [];
    public HashSet<string> GitHubPlugins { get; set; } = [];

    protected override void Init()
    {
        if (LocalPlugins == null)
            LocalPlugins = [];
        if (GitHubPlugins == null)
            GitHubPlugins = [];
    }

}