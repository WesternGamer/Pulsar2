using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PluginLoader2.Plugins;

[MessagePackObject]
public class NuGetPackageId
{
    [Key(0)]
    [XmlElement]
    public string Name { get; set; }

    [IgnoreMember]
    [XmlAttribute("Include")]
    public string NameAttribute {
        get => Name;
        set => Name = value;
    }

    [Key(1)]
    [XmlElement]
    public string Version { get; set; }

    [IgnoreMember]
    [XmlAttribute("Version")]
    public string VersionAttribute {
        get => Version;
        set => Version = value;
    }

}