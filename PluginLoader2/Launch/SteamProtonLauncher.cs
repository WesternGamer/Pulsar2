using CliWrap;
using CliWrap.Buffered;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PluginLoader2.Launch;

internal class SteamProtonLauncher : IGameLauncher
{
    public bool IsValid => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public int Priority => 99;

    public bool CheckIsRunning()
    {
        return true; // TODO
    }

    public async Task<bool> StartGame()
    {
        // TODO: Copy file to a location on the disk (cannot reference files inside process bin folder)

        CommandResult result = await Cli.Wrap("steam")
            .WithArguments(["-applaunch", "1133870", "-plugins:TODO"])
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteAsync(Program.MasterCancelToken.Token);
        return result.IsSuccess;
    }

    private async Task<string> GetWindowsPath(string linuxPath)
    {
        BufferedCommandResult result = await Cli.Wrap("winepath")
            .WithArguments(["-w", linuxPath])
            .WithValidation(CommandResultValidation.ZeroExitCode)
            .ExecuteBufferedAsync(Program.MasterCancelToken.Token);
        return result.StandardOutput;
    }
}