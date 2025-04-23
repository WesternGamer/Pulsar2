using PluginLoader2.Plugins;
using System;
using System.Reflection;

namespace PluginLoader2.Loader.Plugins;

class GitHubPlugin : IPluginInstance
{
    private readonly GitHubPluginData data;

    public GitHubPlugin(GitHubPluginData data)
    {
        this.data = data;
    }

    public Assembly Load()
    {
        throw new NotImplementedException();
    }
}
