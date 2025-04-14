using Avalonia.Controls;
using PluginLoader2.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginLoader2.Launch;

internal class GameLauncher
{
    private readonly LauncherConfig config;

    public GameLauncher(LauncherConfig config)
    {
        this.config = config;
    }

    private IGameLauncher GetLauncher()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        return new IGameLauncher[] { 
            new SteamAppLauncher(config), 
            new SteamProtonLauncher() 
        }.Where(x => x.IsValid).FirstOrDefault();
#pragma warning restore CA1416 // Validate platform compatibility
    }

    public async Task<bool> Launch()
    {
        IGameLauncher gameLauncher = GetLauncher();
        if (gameLauncher.CheckIsRunning())
        {
            await MessageBox.Show("Already Running", "Space Engineers 2 is already running!");
            return false;
        }

        return await gameLauncher.StartGame();
    }
}