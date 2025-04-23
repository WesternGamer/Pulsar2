using PluginLoader2.Plugins;
using System.ComponentModel;

namespace PluginLoader2.ViewModels;

class LocalPluginModel : INotifyPropertyChanged, IPluginModel
{
    private bool enabled;

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
    public string FullPath { get; }
    public string Name { get; }
    public string Version { get; }
    public string FileName { get; }

    public LocalPluginModel() { }

    public LocalPluginModel(LocalPluginData data, bool enabled)
    {
        this.enabled = enabled;
        FullPath = data.FullPath;
        Name = data.Name;
        Version = data.Version;
        FileName = data.FileName;
    }


    public event PropertyChangedEventHandler PropertyChanged;
}
