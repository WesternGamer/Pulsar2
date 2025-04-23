using System.Collections.Generic;
using System;

namespace PluginLoader2.Config;

public class GitHubKey : IEquatable<GitHubKey>
{
    public string PluginId { get; set; }
    public string VersionName { get; set; }

    public GitHubKey() { }

    public GitHubKey(string pluginId, string versionName)
    {
        PluginId = pluginId;
        VersionName = versionName;
    }

    public GitHubKey(string pluginId)
    {
        PluginId = pluginId;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as GitHubKey);
    }

    public bool Equals(GitHubKey other)
    {
        return other is not null &&
               PluginId == other.PluginId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PluginId);
    }

    public static bool operator ==(GitHubKey left, GitHubKey right)
    {
        return EqualityComparer<GitHubKey>.Default.Equals(left, right);
    }

    public static bool operator !=(GitHubKey left, GitHubKey right)
    {
        return !(left == right);
    }
}