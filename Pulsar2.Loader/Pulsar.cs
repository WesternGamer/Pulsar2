using Keen.Game2.Game.Plugins;
using System.IO;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;
using Pulsar2.Config;
using Pulsar2.Loader.SplashScreen;
using System.Windows.Forms;
using Pulsar2.Plugins;
using System.Collections.Generic;
using Pulsar2.Loader.Plugins;
using System.Text;
using Pulsar2.Plugins.List;
using System.Xml.Schema;
using Pulsar2.Loader.Compile;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;


namespace Pulsar2.Loader
{
    public class Pulsar : IPlugin
    {
        private List<PluginRuntime> runningPlugins = new List<PluginRuntime>();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Pulsar(PluginHost host)
        {
            try
            {

                Directory.CreateDirectory(FileUtilities.AppData);

                Log.Init(Path.Combine(FileUtilities.AppData, "logs", "loader.log"));

                ISplashScreen splashScreen = GameSplashScreen.GetSplashScreen();

                GlobalReferences.GenerateAssemblyList();

                Log.Info("Loading config");
                LoaderConfig config = GetConfig(splashScreen);
                if (config == null)
                    return;

                if (splashScreen.IsVisible)
                {
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Pulsar2.Loader.splash.gif");
                    splashScreen.TakeControl(stream, "Loading...");
                }

                Log.Info("Finding plugins...");
                StringBuilder sb = new StringBuilder();
                sb.Append("Enabled plugins: ").AppendLine();
                IEnumerable<PluginRuntime> githubPlugins = GetGithubPlugins(config, sb, HttpUtilities.CreateHttpClient());
                runningPlugins.AddRange(GetLocalPlugins(config, sb).Concat(githubPlugins));
                Log.Info(sb.ToString());

                Log.Info("Creating plugin instances");
                foreach (PluginRuntime plugin in runningPlugins)
                    plugin.Instantiate(host);

                splashScreen.ResetToDefault();
                splashScreen = new NullSplashScreen();

                Log.Info("Plugin Loader started");
            }
            catch (Exception e)
            {

            }
        }

        private IEnumerable<PluginRuntime> GetLocalPlugins(LoaderConfig config, StringBuilder enabledPluginLog)
        {
            enabledPluginLog.AppendLine("  Local:");
            foreach (LocalPluginConfig dll in config.LocalPlugins.Values)
            {
                if (LocalPluginList.TryCreateDllPlugin(dll.FullPath, out LocalPluginData pluginData))
                {
                    yield return new PluginRuntime(new LocalPlugin(pluginData));
                    enabledPluginLog.Append("    ").Append(pluginData.Name).AppendLine();
                }
                else
                {
                    Log.Error("Plugin " + dll + " could not be loaded");
                }
            }
        }

        private IEnumerable<PluginRuntime> GetGithubPlugins(LoaderConfig config, StringBuilder enabledPluginLog, HttpClient web)
        {
            enabledPluginLog.AppendLine("  GitHub:");
            PluginHubList github = new PluginHubList(web);
            PluginHubData hubData = GameUtils.InvokeSync(() => github.GetHubData());
            Dictionary<string, GitHubPluginData> dataById = hubData.GitHubPlugins.ToDictionary(x => x.Id);
            foreach(GitHubPluginConfig githubConfig in config.GitHubPlugins.Values)
            {
                if(dataById.TryGetValue(githubConfig.Id, out GitHubPluginData data))
                {
                    yield return new PluginRuntime(new GitHubPlugin(data, web, githubConfig));
                    enabledPluginLog.Append("    ").Append(data.Name).AppendLine();
                }
                else
                {
                    Log.Error("Plugin " + githubConfig + " was not found on the Plugin Hub");
                }
            }
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

