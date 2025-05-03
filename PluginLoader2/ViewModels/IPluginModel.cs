namespace PluginLoader2.ViewModels
{
    interface IPluginModel
    {
        string Id { get; }
        bool Enabled { get; set; }
    }
}
