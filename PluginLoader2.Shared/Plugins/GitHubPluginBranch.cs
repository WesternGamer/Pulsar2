using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace PluginLoader2.Plugins;

[MessagePackObject]
public class GitHubPluginBranch
{
    [Key(0)]
    public string Id { get; set; }

    [Key(1)]
    public string Name { get; set; }

    [Key(2)]
    public string Commit { get; set; }

    [Key(3)]
    public string Version { get; set; }

    [Key(4)]
    [XmlArray]
    [XmlArrayItem("Directory")]
    public string[] SourceDirectories { get; set; }

    [Key(5)]
    public string AssetFolder { get; set; }

    [Key(6)]
    [XmlElement("PackageReference")]
    public NuGetPackageId[] NuGetReferences { get; set; }

    [Key(7)]
    public bool ImplicitUsings { get; set; }

    [Key(8)]
    public bool Avalonia { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
