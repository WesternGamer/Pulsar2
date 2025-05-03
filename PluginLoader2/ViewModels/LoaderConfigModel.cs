using PluginLoader2.Config;
using PluginLoader2.Plugins;
using PluginLoader2.Plugins.List;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace PluginLoader2.ViewModels;

class LoaderConfigModel : INotifyPropertyChanged
{
    private ObservableCollection<GitHubPluginModel> githubPlugins = [];
    private Dictionary<string, GitHubPluginModel> originalGithubPlugins = [];

    private ObservableCollection<LocalPluginModel> localPlugins = [];
    private Dictionary<string, LocalPluginModel> originalLocalPlugins = [];

    private readonly LoaderConfig config;

    public ObservableCollection<LocalPluginModel> LocalPlugins
    {
        get => localPlugins;
        set
        {
            localPlugins = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalPlugins)));
        }
    }
    public ObservableCollection<GitHubPluginModel> GitHubPlugins
    {
        get => githubPlugins;
        set
        {
            githubPlugins = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GitHubPlugins)));
        }
    }
    

    public bool HasChanged
    {
        get
        {
            return !IsEqual(githubPlugins, originalGithubPlugins) || !IsEqual(localPlugins, originalLocalPlugins);
        }
    
    }

    private bool IsEqual<T>(ObservableCollection<T> values, Dictionary<string, T> originals) where T : IPluginModel
    {
        if (values.Count != originals.Count)
            return false;

        foreach (T value in values)
        {
            if(!originals.TryGetValue(value.Id, out T original) || !original.Equals(value))
                return false;
        }
        return true;
    }

    public LoaderConfigModel() { }

    public LoaderConfigModel(LoaderConfig config, LocalPluginList localList, PluginHubData hubList)
    {
        this.config = config;

        // Initialize originals with list data and user config
        originalLocalPlugins = localList.LocalPlugins.ToDictionary(x => x.FullPath, x => new LocalPluginModel(x, config.LocalPlugins.ContainsKey(x.FullPath)));
        originalGithubPlugins = hubList.GitHubPlugins.ToDictionary(x => x.Id, x => new GitHubPluginModel(x, TryGetOrDefault(config.GitHubPlugins, x.Id)));

        // Create deep copy of originals for user edit
        localPlugins = [.. originalLocalPlugins.Values.Select(x => new LocalPluginModel(x))];
        githubPlugins = [.. originalGithubPlugins.Values.Select(x => new GitHubPluginModel(x))];
    }
    public LoaderConfigModel(LoaderConfig config, LocalPluginList localList, PluginHubData hubList, LoaderConfigModel copyFrom)
    {
        this.config = config;

        // Initialize originals with list data and user config
        originalLocalPlugins = localList.LocalPlugins.ToDictionary(x => x.FullPath, x => new LocalPluginModel(x, config.LocalPlugins.ContainsKey(x.FullPath)));
        originalGithubPlugins = hubList.GitHubPlugins.ToDictionary(x => x.Id, x => new GitHubPluginModel(x, TryGetOrDefault(config.GitHubPlugins, x.Id)));

        // Create deep copy of originals for user edit
        // Prefer to use user config from old LoaderConfigModel if it exists
        Dictionary<string, LocalPluginModel> otherLocalPlugins = copyFrom.localPlugins.ToDictionary(x => x.Id);
        localPlugins.Clear();
        foreach(LocalPluginModel local in originalLocalPlugins.Values)
        {
            if (otherLocalPlugins.TryGetValue(local.Id, out LocalPluginModel otherModel))
                localPlugins.Add(new LocalPluginModel(local.Data, otherModel.Enabled));
            else
                localPlugins.Add(new LocalPluginModel(local));

        }

        Dictionary<string, GitHubPluginModel> otherGithubPlugins = copyFrom.githubPlugins.ToDictionary(x => x.Id);
        githubPlugins.Clear();
        foreach(GitHubPluginModel github in originalGithubPlugins.Values)
        {
            if (otherGithubPlugins.TryGetValue(github.Id, out GitHubPluginModel otherModel))
                githubPlugins.Add(new GitHubPluginModel(github.Data, otherModel.Config));
            else
                githubPlugins.Add(new GitHubPluginModel(github));

        }

    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Discard()
    {
        GitHubPlugins = [.. originalGithubPlugins.Values.Select(x => new GitHubPluginModel(x))];
        LocalPlugins = [.. originalLocalPlugins.Values.Select(x => new LocalPluginModel(x))];
    }

    public void Save()
    {
        originalLocalPlugins = localPlugins.ToDictionary(x => x.Id, x => new LocalPluginModel(x));
        originalGithubPlugins = githubPlugins.ToDictionary(x => x.Id, x => new GitHubPluginModel(x));
        config.LocalPlugins = localPlugins.Where(x => x.Enabled).ToDictionary(x => x.Id, x => x.Config);
        config.GitHubPlugins = githubPlugins.Where(x => x.Enabled).ToDictionary(x => x.Id, x => x.Config);
        config.Save();
    }


    private TVal TryGetOrDefault<TKey, TVal>(Dictionary<TKey, TVal> dict, TKey key)
    {
        if (dict.TryGetValue(key, out TVal val))
            return val;
        return default;
    }
}