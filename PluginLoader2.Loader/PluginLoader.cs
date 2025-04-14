using Keen.Game2.Game.Plugins;
using System.IO;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using PluginLoader2.Config;
using PluginLoader2.Loader.SplashScreen;
using System.Windows.Forms;
using PluginLoader2.Plugins;
using System.Collections.Generic;
using PluginLoader2.Loader.Plugins;
using System.Text;
using PluginLoader2.Plugins.List;
using System.Xml.Schema;


namespace PluginLoader2.Loader
{
    public class PluginLoader : IPlugin
    {
        private List<Plugin> runningPlugins = new List<Plugin>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public PluginLoader(PluginHost host)
        {
            Directory.CreateDirectory(FileUtilities.AppData);

            Log.Init(Path.Combine(FileUtilities.AppData, "logs", "loader.log"));
            Log.Info("Starting Plugin Loader");

            ISplashScreen splashScreen = GameSplashScreen.GetSplashScreen();

            Log.Info("Loading config");
            LoaderConfig config = GetConfig(splashScreen);
            if (config == null)
                return;

            if(splashScreen.IsVisible)
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PluginLoader2.Loader.splash.gif");
                splashScreen.TakeControl(stream, "Loading...");
            }

            Log.Info("Finding plugins...");
            StringBuilder sb = new StringBuilder();
            sb.Append("Enabled plugins: ").AppendLine();
            foreach(string dll in config.LocalPlugins)
            {
                if(LocalPluginList.TryCreateDllPlugin(dll, out LocalPlugin pluginData))
                {
                    runningPlugins.Add(new Plugin(pluginData));
                    sb.Append(pluginData.FullPath).AppendLine();
                }
                else
                {
                    Log.Error("Plugin " + dll + " could not be loaded");
                }
            }
            Log.Info(sb.ToString());

            PluginHubList github = new PluginHubList(HttpUtilities.CreateHttpClient());


            Log.Info("Creating plugin instances");
            foreach(Plugin plugin in runningPlugins)
                plugin.Instantiate(host);

            splashScreen.ResetToDefault();
            splashScreen = new NullSplashScreen();

            Log.Info("Plugin Loader started");
        }


        private LoaderConfig GetConfig(ISplashScreen splashScreen)
        {
            string configFolder = Path.Combine(FileUtilities.AppData, "config");
            Directory.CreateDirectory(configFolder);
            string configFile = Path.Combine(configFolder, "loader.xml");

            LoaderConfig config;
            while (!ConfigFile.TryLoad(configFile, out config))
            {
                DialogResult result = splashScreen.ShowPopup("Failed to load user settings: user config file invalid or inaccessible", MessageBoxButtons.AbortRetryIgnore);
                if (result == DialogResult.Abort)
                {
                    Log.Info("Loading of config file failed, shutting down program");
                    Environment.Exit(0);
                    return null;
                }
                else if (result == DialogResult.Ignore)
                {
                    Log.Info("Loading of config file failed, no plugins will be active");
                    return null;
                }
            }
            return config;
        }
    }
}

