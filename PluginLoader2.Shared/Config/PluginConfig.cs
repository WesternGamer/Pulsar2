using System.Collections.Generic;
using System.Xml.Serialization;

namespace PluginLoader2.Config
{
    public class PluginConfig
    {
        public List<string> ExternalLocalPlugins { get; set; } = new List<string>();
        public List<string> EnabledPlugins { get; set; } = new List<string>();

        [XmlIgnore]
        public ConfigFile Parent { get; private set; }

        public void Init(ConfigFile parent)
        {
            Parent = parent;
        }

        public void Save()
        {
            Parent.Save();
        }
    }
}
