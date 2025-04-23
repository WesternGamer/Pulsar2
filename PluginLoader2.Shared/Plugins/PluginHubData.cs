using MessagePack;
using System.Net.Http.Headers;

namespace PluginLoader2.Plugins
{
    [MessagePackObject(AllowPrivate = true)]
    internal class PluginHubData
    {
        [Key(0)]
        public Hash Tag { get; set; }

        [Key(1)]
        public GitHubPluginData[] GitHubPlugins { get; set; } = [];
        
        public bool HasValidTag()
        {
            return Tag != null && !string.IsNullOrEmpty(Tag.EntityTag);
        }

        [MessagePackObject(AllowPrivate = true)]
        public class Hash
        {

            [Key(0)]
            public string EntityTag { get; set; }

            [Key(1)]
            public bool WeakTag { get; set; }

            [IgnoreMember]
            public EntityTagHeaderValue Header => new EntityTagHeaderValue(EntityTag, WeakTag);

            public Hash()
            {
            }

            public Hash(EntityTagHeaderValue etagHeader)
            {
                EntityTag = etagHeader.Tag;
                WeakTag = etagHeader.IsWeak;
            }

        }
    }
}
