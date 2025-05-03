using PluginLoader2.Config;
using PluginLoader2.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace PluginLoader2.ViewModels;

class GitHubPluginModel : INotifyPropertyChanged, IPluginModel, IEquatable<GitHubPluginModel>
{
    private bool enabled;
    private GitHubPluginBranch version;

    public string Id => Data.Id;

    public bool Enabled
    {
        get => enabled;
        set 
        {
            if (enabled == value)
                return;
            enabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
        }
    }

    public GitHubPluginBranch Version
    {
        get => version;
        set
        {
            if (version == value)
                return;
            version = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Version)));
        }
    }

    public GitHubPluginData Data { get; } = new GitHubPluginData();

    public GitHubPluginConfig Config => new GitHubPluginConfig(Data) { Branch = Version?.Id };

    public GitHubPluginModel() { }

    public GitHubPluginModel(GitHubPluginData data, GitHubPluginConfig enabledConfig)
    {
        if(enabledConfig == null)
        {
            enabled = false;
            Version = data.Versions?.FirstOrDefault();
        }
        else
        {
            enabled = true;
            if (data.Versions != null && data.Versions.Length > 0)
            {
                GitHubPluginBranch configBranch = data.Versions.FirstOrDefault(x => x.Id == enabledConfig.Branch);
                if (configBranch == null)
                    Version = data.Versions[0];
                else
                    Version = configBranch;
            }
        }
        Data = data;
    }

    public GitHubPluginModel(GitHubPluginModel other)
    {
        enabled = other.enabled;
        version = other.version;
        Data = other.Data;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public override bool Equals(object obj)
    {
        return Equals(obj as GitHubPluginModel);
    }

    public bool Equals(GitHubPluginModel other)
    {
        return other is not null &&
               Id == other.Id &&
               enabled == other.enabled &&
               EqualityComparer<GitHubPluginBranch>.Default.Equals(version, other.version);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, enabled, version);
    }

    public static bool operator ==(GitHubPluginModel left, GitHubPluginModel right)
    {
        return EqualityComparer<GitHubPluginModel>.Default.Equals(left, right);
    }

    public static bool operator !=(GitHubPluginModel left, GitHubPluginModel right)
    {
        return !(left == right);
    }
}
