using Avalonia.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace PluginLoader2.Launch;

internal static class SteamAppFinder
{
    public static async Task<string> FindSteamApp(Window popupOwner)
    {
        string installPath;
        if (!TryFindInstallDir(out installPath))
        {
            /*MessageBoxCustomParams boxSettings = new MessageBoxCustomParams
            {
                ButtonDefinitions = new List<ButtonDefinition>
                    {
                        new ButtonDefinition { Name = "OK", },
                        new ButtonDefinition { Name = "Cancel", }
                    },
                ContentTitle = "Space Engineers Location",
                ContentMessage = "Unable to locate Space Engineers 2 automatically. Please specify it below.",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowInCenter = true,
                Topmost = true,
                InputParams = new InputParams()
            };

            if (!string.IsNullOrWhiteSpace(installPath))
                boxSettings.InputParams.DefaultValue = installPath;

            while (true)
            {
                var locationBox = MessageBoxManager.GetMessageBoxCustom(boxSettings);
                if (await locationBox.ShowAsPopupAsync(popupOwner) != "OK")
                    return null;

                installPath = locationBox.InputValue;
                if (IsValidInstallDir(ref installPath))
                    return installPath;
            }*/

        }
        return installPath;
    }

    private static bool IsValidInstallDir(ref string installPath)
    {
        if (string.IsNullOrWhiteSpace(installPath))
            return false;

        installPath = installPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!Directory.Exists(installPath))
            return false;

        string file = Path.GetFileName(installPath);
        if (file == "Space Engineers 2")
            installPath = Path.Combine(installPath, "Game2");
        else if (file != "Game2")
            return false;
        return File.Exists(Path.Combine(installPath, "SpaceEngineers2.exe"));

    }

    private static bool TryFindInstallDir(out string installPath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            installPath = null;
            return false;
        }

        try
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 1133870"))
            {
                if (key != null)
                {
                    object o = key.GetValue("InstallLocation");
                    if (o != null)
                    {
                        installPath = o.ToString();
                        return IsValidInstallDir(ref installPath);
                    }
                }
            }
        }
        catch (Exception)
        {

        }

        installPath = null;
        return false;
    }
}