using MessagePack;

namespace PluginLoader2.Plugins;

[MessagePackObject]
public class GitHubPluginData
{
    [Key(0)]
    public string Id { get; set; }
    [Key(1)]
    public string Username { get; set; }
    [Key(2)]
    public string Repository { get; set; }
    [Key(3)]
    public string Name { get; set; }
    [Key(4)]
    public string Author { get; set; }
    [Key(5)]
    public string ShortDescription { get; set; }
    [Key(6)]
    public string LongDescription { get; set; }
    [Key(7)]
    public GitHubPluginBranch[] Versions { get; set; }


}