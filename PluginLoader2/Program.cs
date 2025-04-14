using Avalonia;
using Microsoft.Extensions.Logging;
using PluginLoader2.Config;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Velopack;

namespace PluginLoader2;

internal class Program
{

    public static CancellationTokenSource MasterCancelToken { get; } = new CancellationTokenSource(); 

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        Directory.CreateDirectory(FileUtilities.AppData);

        Log.Init(Path.Combine(FileUtilities.AppData, "logs", "launcher.log"));

        using (ILoggerFactory logFactory = new LoggerFactory())
        {
            Log.Link(logFactory, "Velopack");
            VelopackApp.Build()
                .Run(logFactory.CreateLogger("Velopack"));

            await CheckForUpdates();
        }


        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task CheckForUpdates()
    {
        try
        {
            UpdateManager mgr = new UpdateManager("https://api.sepluginloader.com/download");

            if (!mgr.IsInstalled)
                return;

            // check for new version
            UpdateInfo newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
                return; // no update available

            // download new version
            await mgr.DownloadUpdatesAsync(newVersion);

            // install new version and restart app
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to check for updates: ", ex);
        }
    }

    private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
    {
        MasterCancelToken.Cancel();
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}