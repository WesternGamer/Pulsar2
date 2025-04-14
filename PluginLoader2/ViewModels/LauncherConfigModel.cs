using PluginLoader2.Config;
using PluginLoader2.Plugins;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PluginLoader2.ViewModels;

class LauncherConfigModel : INotifyPropertyChanged
{
    private bool disableAutoStart;
    private ObservableCollection<string> localFolders = [];
    private readonly LauncherConfig config;

    public bool DisableAutoStart
    {
        get => disableAutoStart;
        set 
        {
            disableAutoStart = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisableAutoStart)));
        }
    }

    public ObservableCollection<string> LocalPluginRepositories 
    {
        get => localFolders;
        set 
        {
            localFolders = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalPluginRepositories)));
        }
    }

    public LauncherConfigModel() { }

    public LauncherConfigModel(LauncherConfig config)
    {
        disableAutoStart = config.DisableAutoStart;
        localFolders = new ObservableCollection<string>(config.LocalPluginRepositories);
        this.config = config;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void Discard()
    {
        DisableAutoStart = config.DisableAutoStart;
        LocalPluginRepositories = new ObservableCollection<string>(config.LocalPluginRepositories);
    }

    public void Save()
    {
        config.DisableAutoStart = disableAutoStart;
        config.LocalPluginRepositories = new List<string>(localFolders);
        config.Save();
    }
}