using CliWrap;
using CliWrap.Exceptions;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace PluginLoader2;

static class PlatformTools
{
    public async static Task<bool> ShowDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return false;

        try
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                await Cli.Wrap("explorer.exe")
                    .WithArguments([directory])
                    .WithValidation(CommandResultValidation.ZeroExitCode)
                    .ExecuteAsync(cts.Token);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await Cli.Wrap("xdg-open")
                    .WithArguments([directory])
                    .WithValidation(CommandResultValidation.ZeroExitCode)
                    .ExecuteAsync(cts.Token);
                return true;
            }
        }
        catch (Exception e)
        {
            Log.Warn("Exception while trying to show directory in explorer:", e);
        }
        return false;
    }

    public async static Task<bool> ShowFile(string file)
    {
        if (!File.Exists(file))
            return false;


        try
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Command cmd = Cli.Wrap("explorer.exe")
                    .WithArguments($"/select,\"{file}\"")
                    .WithValidation(CommandResultValidation.None);
                CommandResult result = await cmd.ExecuteAsync(cts.Token);

                if(result.ExitCode == 0 || result.ExitCode == 1)
                    return true;
                throw new CommandExecutionException(
                    cmd,
                    result.ExitCode,
                    $"Command execution failed because the underlying process returned a non-zero exit code ({result.ExitCode}). Command:{cmd.TargetFilePath} {cmd.Arguments} on the command."
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string directory = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(directory))
                    return false;

                await Cli.Wrap("xdg-open")
                    .WithArguments([directory])
                    .WithValidation(CommandResultValidation.ZeroExitCode)
                    .ExecuteAsync(cts.Token);
                return true;
            }
        }
        catch (Exception e)
        {
            Log.Warn("Exception while trying to show directory in explorer:", e);
        }
        return false;
    }

    public async static Task<bool> OpenBrowser(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            return false;

        try
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Command cmd = Cli.Wrap("explorer.exe")
                    .WithArguments($"\"{uri.ToString()}\"")
                    .WithValidation(CommandResultValidation.None);
                CommandResult result = await cmd.ExecuteAsync(cts.Token);

                if (result.ExitCode == 0 || result.ExitCode == 1)
                    return true;
                throw new CommandExecutionException(
                    cmd,
                    result.ExitCode,
                    $"Command execution failed because the underlying process returned a non-zero exit code ({result.ExitCode}). Command:{cmd.TargetFilePath} {cmd.Arguments} on the command."
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await Cli.Wrap("xdg-open")
                    .WithArguments([uri.ToString()])
                    .WithValidation(CommandResultValidation.ZeroExitCode)
                    .ExecuteAsync(cts.Token);
                return true;
            }
        }
        catch (Exception e)
        {
            Log.Warn("Exception while trying to open url in browser:", e);
        }


        return false;
    }
}

