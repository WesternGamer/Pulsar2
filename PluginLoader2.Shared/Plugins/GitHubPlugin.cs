using MessagePack;
using System;
using System.Reflection;

namespace PluginLoader2.Plugins;

[MessagePackObject(AllowPrivate = true)]
public class GitHubPlugin : ICustomPlugin
{
    [Key(0)]
    public string Id { get; set; }
    [Key(1)]
    public string Username { get; set; }
    [Key(2)]
    public string Repository { get; set; }
    [Key(3)]
    public string Commit { get; set; }
    [Key(4)]
    public string Name { get; set; }
    [Key(5)]
    public string Author { get; set; }
    [Key(6)]
    public string Version { get; set; }
    [Key(7)]
    public string ShortDescription { get; set; }
    [Key(8)]
    public string LongDescription { get; set; }

    public Assembly GetAssembly(IAssemblyResolver resolver)
    {
        throw new NotImplementedException();
    }
}