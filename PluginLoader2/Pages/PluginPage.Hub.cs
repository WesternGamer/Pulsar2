using Avalonia.Interactivity;
using Avalonia.Threading;
using HarfBuzzSharp;
using MessagePack;
using PluginLoader2.Config;
using PluginLoader2.Plugins;
using PluginLoader2.Plugins.List;
using PluginLoader2.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PluginLoader2.Pages;

partial class PluginsPage
{
    private static PluginHubData pluginHubData = null;

    private async Task<PluginHubData> DownloadPluginList(CancellationToken cancelToken)
    {
        if (pluginHubData != null)
            return pluginHubData;

        PluginHubList hubList = new PluginHubList();
        hubList.OnGitHubPluginDownloaded += OnGitHubPluginAdded;
        try
        {
            pluginHubData = await hubList.GetHubData(cancelToken);
        }
        catch (OperationCanceledException)
        {
            hubList.OnGitHubPluginDownloaded -= OnGitHubPluginAdded;
            return null;
        }
        hubList.OnGitHubPluginDownloaded -= OnGitHubPluginAdded;
        return pluginHubData ?? new PluginHubData();

    }

    private void OnGitHubPluginAdded(GitHubPluginData plugin)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (loaderConfig.GitHubPlugins.TryGetValue(plugin.Id, out GitHubPluginConfig config))
                context.GitHubPlugins.Insert(0, new GitHubPluginModel(plugin, config));
            else
                context.GitHubPlugins.Add(new GitHubPluginModel(plugin, null));
        });
    }


    private async void OpenGitHubPluginClicked(object sender, RoutedEventArgs e)
    {
        if (githubPluginsGrid.SelectedItem is GitHubPluginModel plugin)
        {
            await PlatformTools.OpenBrowser($"https://github.com/{plugin.Data.Username}/{plugin.Data.Repository}");
        }
    }

    private void HubPluginSelectionChanged(object sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (githubPluginsGrid.SelectedItem is GitHubPluginModel plugin)
        {
            hubPluginDetailsPane.IsVisible = true;
            hubPluginDetailsPane.DataContext = plugin;
        }
        else
        {
            hubPluginDetailsPane.IsVisible = false;
        }
    }

    private void GitHubPluginBranchChanged(object sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        
        PromptSave();
    }
}
