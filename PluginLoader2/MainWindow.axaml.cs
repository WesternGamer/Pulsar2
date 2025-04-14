using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using PluginLoader2.Config;
using System.IO;
using System.Threading.Tasks;
using PluginLoader2.Launch;
using System.Linq;
using System.Reflection;
using FluentAvalonia.UI.Windowing;
using FluentAvalonia.UI.Controls;
using PluginLoader2.Pages;

namespace PluginLoader2;

public partial class MainWindow : AppWindow
{
    private readonly ProcessLaunchOptions launchOptions = new ProcessLaunchOptions();
    private LauncherConfig config;
    private LoaderConfig loaderConfig;

    public MainWindow()
    {
        InitializeComponent();

        launchOptions = new ProcessLaunchOptions([]);
    }

    public MainWindow(ProcessLaunchOptions options)
    {
        InitializeComponent();

        launchOptions = options;
    }

    private async void OnWindowInit(object sender, RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
        {
            mainMenu.Content = new HomePage();
            return;
        }

        string configFolder = Path.Combine(FileUtilities.AppData, "config");
        Directory.CreateDirectory(configFolder);

        if (!ConfigFile.TryLoad(Path.Combine(configFolder, "launcher.xml"), out config))
        {
            config = new LauncherConfig();
            await MessageBox.Show("User Config Invalid", "Failed to load user settings");
        }

        if (!ConfigFile.TryLoad(Path.Combine(configFolder, "loader.xml"), out loaderConfig))
        {
            loaderConfig = new LoaderConfig();
            await MessageBox.Show("User Config Invalid", "Failed to load user plugin settings");
        }

        mainMenu.Content = new HomePage(launchOptions, config, loaderConfig);
    }



    private void OnMenuSelectionChanged(object sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (e.SelectedItem is NavigationViewItem nvi)
        {
            launchOptions.DisableAutoStart = true;
            Control content;
            switch (nvi.Tag)
            {
                case "Home":
                    content = new HomePage(launchOptions, config, loaderConfig);
                    break;
                case "Plugins":
                    content = new PluginsPage(loaderConfig, config);
                    break;
                case "Settings":
                    content = new SettingsPage(config);
                    break;
                default:
                    return;
            }
            mainMenu.Content = content;
        }
    }
}