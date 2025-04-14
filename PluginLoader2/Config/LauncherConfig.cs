using System.Collections.Generic;
using System.IO;

namespace PluginLoader2.Config;

public class LauncherConfig : ConfigFile
{
    public string SteamInstall { get; set; }
    public bool DisableAutoStart { get; set; }
    public List<string> LocalPluginRepositories { get; set; } = [];

    protected override void Init()
    {
        if (LocalPluginRepositories == null)
            LocalPluginRepositories = [];

        for (int i = LocalPluginRepositories.Count - 1; i >= 0; i--)
        {
            string folder = LocalPluginRepositories[i];
            if (!Directory.Exists(folder))
                LocalPluginRepositories.RemoveAt(i);
        }
    }
}