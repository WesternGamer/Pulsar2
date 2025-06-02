using Pulsar2.Plugins;

namespace Pulsar2.Config;

public class GitHubPluginConfig
{
    public GitHubPluginConfig()
    {

    }

    public GitHubPluginConfig(string id)
    {
        Id = id;
    }

    public GitHubPluginConfig(string id, string name) : this(id)
    {
        Name = name;
    }

    public GitHubPluginConfig(GitHubPluginData data) : this(data.Id, data.Name)
    {

    }

    public string Id { get; set; }

    public string Name { get; set; } // Used for display of plugin to user

    public string Branch { get; set; }

}