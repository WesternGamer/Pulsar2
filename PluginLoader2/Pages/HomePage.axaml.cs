using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using System;
using Avalonia.Threading;
using PluginLoader2.Launch;
using PluginLoader2.Config;
using System.IO;
using PluginLoader2.Plugins;

namespace PluginLoader2.Pages;

public partial class HomePage : UserControl
{
    private const int StartTimerLength = 5;

    private DispatcherTimer startTimer = new DispatcherTimer();
    private DateTime timerStarted;
    private GameLauncher steam;

    public HomePage()
    {
        InitializeComponent();
    }

    public HomePage(ProcessLaunchOptions launchOptions, LauncherConfig config, LoaderConfig loaderConfig)
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        PopulatePluginList(loaderConfig);

        steam = new GameLauncher(config);
        if (launchOptions.SkipLauncher)
        {
            Initialized += StartAfterInit;
        }
        else if (launchOptions.DisableAutoStart || config.DisableAutoStart)
        {
            btnStart.Content = "Start Game";
        }
        else
        {
            timerStarted = DateTime.UtcNow;
            StartTimer_Tick(null, null);
            startTimer.Interval = TimeSpan.FromSeconds(1);
            startTimer.Tick += StartTimer_Tick;
            startTimer.Start();
        }
    }

    private void OnPageUnload(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        startTimer.Stop();
        startTimer.Tick -= StartTimer_Tick;
    }


    private void PopulatePluginList(LoaderConfig loaderConfig)
    {
        enabledPluginsList.Items.Clear();

        if(loaderConfig.LocalPlugins.Count == 0)
        {
            PopulatePluginList("None");
            return;
        }

        foreach(string plugin in loaderConfig.LocalPlugins)
            PopulatePluginList(Path.GetFileName(plugin), plugin);

        foreach(string plugin in loaderConfig.GitHubPlugins) // TODO: Store plugin names for display to the user
            PopulatePluginList(plugin, "GitHub: " + plugin);
    }

    private void PopulatePluginList(string text, string tooltip = null)
    {
        var item = new ListBoxItem()
        {
            Content = text,
        };
        enabledPluginsList.Items.Add(item);
        if (!string.IsNullOrEmpty(tooltip))
            ToolTip.SetTip(item, tooltip);
    }

    private async void StartAfterInit(object sender, EventArgs e)
    {
        await StartGame();
    }

    private async void StartTimer_Tick(object sender, EventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        TimeSpan elapsed = DateTime.UtcNow - timerStarted;
        int seconds = (int)Math.Ceiling(StartTimerLength - elapsed.TotalSeconds);
        if (seconds <= 0)
        {
            startTimer.Stop();
            await StartGame();
            return;
        }

        btnStart.Content = $"Starting in {seconds}...";
    }

    private async void OnMainButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;

        if (startTimer.IsEnabled)
        {
            startTimer.Stop();
            btnStart.Content = "Start Game";
        }
        else
        {
            await StartGame();
        }
    }


    private async Task StartGame()
    {
        if (Design.IsDesignMode)
            return;

        // TODO: Prevent user interaction with the window while game is starting?

        startTimer.Stop();
        btnStart.Content = $"Starting...";
        if (await steam.Launch())
        {
            Log.Info("Started SE, closing.");
            Environment.Exit(0);
            return;
        }
        btnStart.Content = "Start Game";
    }

}