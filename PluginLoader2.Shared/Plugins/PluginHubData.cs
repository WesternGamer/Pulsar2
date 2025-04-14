using MessagePack;

namespace PluginLoader2.Plugins
{
    [MessagePackObject(AllowPrivate = true)]
    internal class PluginHubData
    {
        [Key(0)]
        public string Hash { get; set; }

        [Key(1)]
        public GitHubPlugin[] GitHubPlugins { get; set; } = [];
    }
}
