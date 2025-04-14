using CliWrap;
using Microsoft.Win32;
using PluginLoader2.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace PluginLoader2.Launch;

[SupportedOSPlatform("windows")]
internal class SteamAppLauncher : IGameLauncher
{
    private readonly LauncherConfig config;

    public bool IsValid => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public int Priority => 100;

    public SteamAppLauncher(LauncherConfig config)
    {
        this.config = config;
    }

    public bool CheckIsRunning()
    {
        return Process.GetProcessesByName("SpaceEngineers2").Any();
    }

    public async Task<bool> StartGame()
    {
        string steamExe = await EnsureSteamRunning();
        if (steamExe == null)
        {
            await MessageBox.Show("Steam Not Running", "Unable to locate Steam. Please ensure that it is running before starting the game.");
            return false;
        }

        bool userReady = await EnsureSteamUserReady();
        if (!userReady)
        {
            await MessageBox.Show("Steam Not Ready", "Unable to locate Steam user. Please ensure that you are signed into Steam before starting the game.");
            return false;
        }

        string loaderDll = GetLoaderDll();

        try
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await Cli.Wrap(steamExe)
                .WithArguments(["-applaunch", "1133870", "-plugins:" + loaderDll])
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log.Error("Timed out waiting for SE2 to start");
            return false;
        }
        catch (Exception e)
        {
            Log.Error("Exception while starting SE2: ", e);
            return false;
        }

        return true;
    }

    private static string GetLoaderDll()
    {
        string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string loaderDll;
#if DEBUG
        loaderDll = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\..\..\PluginLoader2.Init\bin\x64\Debug\net8.0-windows\PluginLoader2.Init.dll"));
#else
        loaderDll = Path.Combine(currentPath, "loader", "PluginLoader2.Init.dll");
#endif
        return loaderDll;
    }

    private async Task<string> EnsureSteamRunning()
    {
        string steamExe;

        if (IsSteamRunning(out steamExe))
            return steamExe;

        try
        {
            await Cli.Wrap("cmd.exe")
                .WithArguments("/c start steam://")
                .WithValidation(CommandResultValidation.ZeroExitCode)
                .ExecuteAsync();
        }
        catch (Exception e)
        {
            Log.Error("Exception while starting Steam: ", e);
            return null;
        }

        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(1000);
            if (IsSteamRunning(out steamExe))
                return steamExe;
        }

        return null;
    }

    private static bool IsValidInstallFile(string installFile)
    {
        return !string.IsNullOrWhiteSpace(installFile) && File.Exists(installFile) && Path.GetFileName(installFile).Equals("steam.exe", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsSteamRunning(out string location)
    {
        location = null;
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam\\ActiveProcess"))
            {
                if (key?.GetValue("pid") is int processId && processId != 0)
                {
                    if (!TryGetProcessById(processId, out Process steam))
                        return false;

                    location = steam.MainModule?.FileName;
                    return IsValidInstallFile(location);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error("Exception while searching for steam process: ", e);
        }

        return false;
    }

    private static bool TryGetProcessById(int id, out Process process)
    {
        process = Process.GetProcesses().FirstOrDefault(x => x.Id == id);
        return process != null;
    }

    private static async Task<bool> EnsureSteamUserReady()
    {
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(1000);
            if (IsSteamUserReady())
                return true;
        }
        return false;
    }
        
    private static bool IsSteamUserReady()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Valve\\Steam\\ActiveProcess"))
            {
                if (key?.GetValue("ActiveUser") is int userId)
                    return userId != 0;
            }
        }
        catch (Exception e)
        {
            Log.Error("Exception while searching for steam user: ", e);
        }

        return false;
    }

}