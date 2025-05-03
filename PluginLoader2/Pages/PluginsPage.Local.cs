using Avalonia.Controls;
using Avalonia.Threading;
using PluginLoader2.Config;
using PluginLoader2.Plugins;
using PluginLoader2.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PluginLoader2.Pages;

partial class PluginsPage
{
    private async Task<bool> RefreshLocalPlugins(bool init, CancellationToken cancelToken)
    {
        localPlugins.Clear();
        if(init)
            localPlugins.OnPluginAdded += OnLocalPluginAdded;

        try
        {
            await localPlugins.AddPluginsAsync(cancelToken);
            foreach (string folder in launcherConfig.LocalPluginRepositories)
                await localPlugins.AddPluginsAsync(folder, cancelToken: cancelToken);
            await localPlugins.AddPluginsAsync(loaderConfig.LocalPlugins.Values.Select(x => x.FullPath), cancelToken);
        }
        catch(OperationCanceledException)
        {
            localPlugins.OnPluginAdded -= OnLocalPluginAdded;
            return false;
        }

        localPlugins.OnPluginAdded -= OnLocalPluginAdded;
        return true;
    }

    private void OnLocalPluginAdded(LocalPluginData plugin)
    {
        // This is called during loading of the local plugin list
        Dispatcher.UIThread.Post(() =>
        {
            if (loaderConfig.LocalPlugins.ContainsKey(plugin.FullPath))
                context.LocalPlugins.Insert(0, new LocalPluginModel(plugin, true));
            else
                context.LocalPlugins.Add(new LocalPluginModel(plugin, false));

        });
    }


    private async void OnOpenLocalPluginFolderClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        string directory = Path.Combine(FileUtilities.AppData, "plugins", "local");
        Directory.CreateDirectory(directory);
        if (!await PlatformTools.ShowDirectory(directory))
            await MessageBox.Show("Local Plugins Folder", "Local plugins can be placed into the following folder: " + directory);
    }


    private async void OnRefreshLocalPluginFolderClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await RefreshModel();
    }

    private async void OpenPluginMenuItemClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if(localPluginsGrid.SelectedItem is LocalPluginModel plugin)
            await PlatformTools.ShowFile(plugin.Data.FullPath);
    }

}
