using PluginLoader2.Config;
using PluginLoader2.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PluginLoader2.ViewModels;

class LocalPluginModel : INotifyPropertyChanged, IPluginModel, IEquatable<LocalPluginModel>
{
    private bool enabled;

    public string Id => Data.FullPath;

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

    public LocalPluginData Data { get; } = new LocalPluginData();

    public LocalPluginConfig Config => new LocalPluginConfig(Data);

    public LocalPluginModel() { }

    public LocalPluginModel(LocalPluginData data, bool enabled)
    {
        this.enabled = enabled;
        Data = data;
    }

    public LocalPluginModel(LocalPluginModel other)
    {
        enabled = other.enabled;
        Data = other.Data;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public override bool Equals(object obj)
    {
        return Equals(obj as LocalPluginModel);
    }

    public bool Equals(LocalPluginModel other)
    {
        return other is not null &&
               Id == other.Id &&
               enabled == other.enabled;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, enabled);
    }

    public static bool operator ==(LocalPluginModel left, LocalPluginModel right)
    {
        return EqualityComparer<LocalPluginModel>.Default.Equals(left, right);
    }

    public static bool operator !=(LocalPluginModel left, LocalPluginModel right)
    {
        return !(left == right);
    }
}
