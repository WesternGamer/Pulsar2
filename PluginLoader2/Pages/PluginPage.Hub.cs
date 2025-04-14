using Avalonia.Interactivity;
using Avalonia.Threading;
using HarfBuzzSharp;
using MessagePack;
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
#if DEBUG
            GitHubPlugin testPlugin = new GitHubPlugin()
            {
                Id = "asdfjkl",
                Name = "Test plugin",
                Username = "sepluginloader",
                Repository = "PluginHub2",
                Author = "avaness",
                ShortDescription = "Allows scrolling between welder, grinder, and drill.",
                LongDescription = "This plugin is a simplified version of the Tool Switcher mod. It allows you to scroll between the welder, grinder, and drill. There is no configuration yet, so these tools will always be grouped together while the plugin is enabled. The benefits of this plugin over the mod are that you can use any toolbar page, and it will work on any multiplayer server."
            };
            XmlSerializer xml = new XmlSerializer(typeof(GitHubPlugin));
            using (FileStream fs = File.OpenWrite(@"D:\Downloads\pluginhub2test\test.xml"))
                xml.Serialize(fs, testPlugin);
            pluginHubData.GitHubPlugins = new List<GitHubPlugin>(pluginHubData.GitHubPlugins.Append(testPlugin)).ToArray();
#endif
        }
        catch (OperationCanceledException)
        {
            hubList.OnGitHubPluginDownloaded -= OnGitHubPluginAdded;
            return null;
        }
        hubList.OnGitHubPluginDownloaded -= OnGitHubPluginAdded;
        return pluginHubData ?? new PluginHubData();

    }

    private void OnGitHubPluginAdded(GitHubPlugin plugin)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (loaderConfig.GitHubPlugins.Contains(plugin.Id))
                context.GitHubPlugins.Insert(0, new GitHubPluginModel(plugin, true));
            else
                context.GitHubPlugins.Add(new GitHubPluginModel(plugin, false));
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
}
