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
    private ObservableCollection<LocalPluginModel> localPlugins = [];
    private readonly LoaderConfig config = new LoaderConfig();
    private readonly LocalPluginList localList;
    private readonly PluginHubData hubList;

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
            if (!LocalEnabled.SetEquals(config.LocalPlugins))
                return true;
            return !GitHubEnabled.SetEquals(config.GitHubPlugins);
        }
    
    }

    private HashSet<string> LocalEnabled => localPlugins.Where(x => x.Enabled).Select(x => x.FullPath).ToHashSet();
    private HashSet<string> GitHubEnabled => githubPlugins.Where(x => x.Enabled).Select(x => x.Data.Id).ToHashSet();

    public LoaderConfigModel() { }

    public LoaderConfigModel(LoaderConfig config, LocalPluginList localList, PluginHubData hubList)
    {
        this.config = config;
        this.localList = localList;
        this.hubList = hubList;
        localPlugins = [.. GetModel(localList.LocalPlugins, config.LocalPlugins)];
        githubPlugins = [.. GetModel(hubList.GitHubPlugins, config.GitHubPlugins)];
    }
    public LoaderConfigModel(LoaderConfig config, LocalPluginList localList, PluginHubData hubList, LoaderConfigModel copyFrom)
    {
        this.config = config;
        this.localList = localList;
        this.hubList = hubList;
        localPlugins = [.. GetModel(localList.LocalPlugins, LocalEnabled)];
        githubPlugins = [.. GetModel(hubList.GitHubPlugins, GitHubEnabled)];
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Discard()
    {
        LocalPlugins = [.. GetModel(localList.LocalPlugins, config.LocalPlugins)];
        GitHubPlugins = [.. GetModel(hubList.GitHubPlugins, config.GitHubPlugins)];
    }

    public void Save()
    {
        config.LocalPlugins = LocalEnabled;
        config.GitHubPlugins = GitHubEnabled;
        config.Save();
    }

    private IEnumerable<LocalPluginModel> GetModel(IEnumerable<LocalPlugin> plugins, HashSet<string> enabledPlugins)
    {
        return plugins.Select(x => new LocalPluginModel(x, enabledPlugins.Contains(x.FullPath))).OrderByDescending(x => x.Enabled);
    }
    private IEnumerable<GitHubPluginModel> GetModel(IEnumerable<GitHubPlugin> plugins, HashSet<string> enabledPlugins)
    {
        return plugins.Select(x => new GitHubPluginModel(x, enabledPlugins.Contains(x.Id))).OrderByDescending(x => x.Enabled);
    }
}