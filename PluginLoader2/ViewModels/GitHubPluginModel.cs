using PluginLoader2.Plugins;
using System.ComponentModel;

namespace PluginLoader2.ViewModels;

class GitHubPluginModel : INotifyPropertyChanged, IPluginModel
{
    private bool enabled;

    public bool Enabled {
        get => enabled;
        set {
            if (enabled == value)
                return;
            enabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Enabled)));
        }
    }

    public GitHubPlugin Data { get; } = new GitHubPlugin();
    public string Name { get; }

    public GitHubPluginModel() { }

    public GitHubPluginModel(GitHubPlugin data, bool enabled)
    {
        this.enabled = enabled;
        Data = data;
    }


    public event PropertyChangedEventHandler PropertyChanged;
}
