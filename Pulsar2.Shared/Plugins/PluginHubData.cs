using MessagePack;
using System.Net.Http.Headers;

namespace Pulsar2.Plugins
{
    [MessagePackObject(AllowPrivate = true)]
    internal class PluginHubData
    {
        [Key(0)]
        public string Hash { get; set; }

        [Key(1)]
        public GitHubPluginData[] GitHubPlugins { get; set; } = [];
    }
}
