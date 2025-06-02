using MessagePack;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Pulsar2.Plugins.List;

class PluginHubList
{
    public const string listRepoCacheFile = "pluginhub.bin";
    private const string pluginHubHash = "https://raw.githubusercontent.com/sepluginloader/PulsarHub2/refs/heads/main/plugins.sha1";
    private const string pluginHub = "https://github.com/sepluginloader/PulsarHub2/archive/main.zip";

    private readonly HttpClient web;

    public event Action<GitHubPluginData> OnGitHubPluginDownloaded;

    public PluginHubList() : this(new HttpClient())
    { }

    public PluginHubList(HttpClient web)
    {
        this.web = web;
    }

    public async Task<PluginHubData> GetHubData(CancellationToken cancelToken = default)
    {
        Task<PluginHubData> readCacheTask = ReadPluginCache(cancelToken);
        Task<string> getHashTask = GetHubHash(cancelToken);
        await Task.WhenAll(readCacheTask, getHashTask);
        PluginHubData cache = await readCacheTask;
        string hash = await getHashTask;

        if(cache == null && hash == null)
        {
            Log.Error("Failed to load Plugin Hub data");
            return null;
        }

        if(cache != null && cache.Hash == hash)
        {
            Log.Info("Using cached Plugin Hub content");
            cache.GitHubPlugins = cache.GitHubPlugins.Concat(DebugPlugin()).ToArray();
            return cache;
        }

        Log.Info("Downloading Plugin Hub zip file");

        PluginHubData latestHubData = await GetHubFile(cancelToken);

        if (latestHubData == null)
        {
            if (cache == null)
            {
                Log.Error("Failed to load Plugin Hub data");
                return null;
            }

            Log.Info("Using cached Plugin Hub content");
            return cache;
        }

        latestHubData.Hash = hash;
        await SaveHubFile(latestHubData, cancelToken);
        return latestHubData;
    }

    private IEnumerable<GitHubPluginData> DebugPlugin()
    {
#if DEBUG
        GitHubPluginData testPlugin = new GitHubPluginData()
        {
            Id = "DevToolEnabled",
            Name = "Avalonia DevTools",
            Username = "ArthurGamerHD",
            Repository = "SE2-UI-DevTool-Enabler",
            Author = "Arthur",
            ShortDescription = "Enables Avalonia Dev Tools (by pressing Shift+F12) in game",
            LongDescription = "Enables Avalonia Dev Tools (by pressing Shift+F12) in game",
            Versions =
            [
                new GitHubPluginBranch()
                {
                    Id = "main",
                    Name = "Main",
                    Commit = "d74abbbb01640e0f0aaae9dca4beec161e28560a",
                    Version = "1.0.0",
                    ImplicitUsings = true,
                }
            ]
        };
        GitHubPluginData testPlugin2 = new GitHubPluginData()
        {
            Id = "SE2ExtendedPaintingUI",
            Name = "Extended Color UI",
            Username = "ArthurGamerHD",
            Repository = "SE2ExtendedPaintingUI",
            Author = "Arthur",
            ShortDescription = "A color picker to all your color needs",
            LongDescription = "A color picker to all your color needs",
            Versions =
            [
                new GitHubPluginBranch()
                {
                    Id = "main",
                    Name = "Main",
                    Commit = "00b6d5932ee0964a7d82ec9cf730eb4689b97382",
                    Version = "1.0.0",
                    Avalonia = true,
                }
            ]
        };

        return [testPlugin, testPlugin2];
#endif
    }

    private async Task<string> GetHubHash(CancellationToken cancelToken)
    {
        try
        {
            Log.Info("Downloading " + pluginHubHash);
            string result = await web.GetStringAsync(pluginHubHash, cancelToken);
            if (string.IsNullOrEmpty(result))
                return null;
            return result.Trim();
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch(Exception ex)
        {
            Log.Error("Error while downloading plugin hub hash: ", ex);
            return null;
        }
    }

    private async Task<PluginHubData> GetHubFile(CancellationToken cancelToken = default)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, pluginHub);
        
        List<GitHubPluginData> plugins = new List<GitHubPluginData>();

        try
        {
            using HttpResponseMessage response = await web.SendAsync(request, cancelToken);
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Error while downloading plugin data from hub: Response code: " + response.StatusCode);
                return null;
            }

            using Stream zipFileStream = await response.Content.ReadAsStreamAsync(cancelToken);
            using ZipArchive zipFile = new ZipArchive(zipFileStream);
            ParseZipFile(plugins, zipFile);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Error while downloading plugin data from hub: ", ex);
            return null;
        }

        return new PluginHubData()
        {
            GitHubPlugins = plugins.ToArray()
        };
    }

    private void ParseZipFile(List<GitHubPluginData> plugins, ZipArchive zipFile)
    {
        XmlSerializer xml = new XmlSerializer(typeof(GitHubPluginData));
        foreach (ZipArchiveEntry entry in zipFile.Entries)
        {
            if (!entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] entryPath = entry.FullName.Split(['/', '\\'], 3);
            if (entryPath.Length < 3 || !entryPath[1].Equals("plugins", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                using Stream entryStream = entry.Open();
                using StreamReader entryReader = new StreamReader(entryStream);

                GitHubPluginData data = (GitHubPluginData)xml.Deserialize(entryReader);
                OnGitHubPluginDownloaded?.Invoke(data);
                plugins.Add(data);
            }
            catch (InvalidOperationException e)
            {
                Log.Error("An error occurred while reading " + entry.FullName + ": " + (e.InnerException ?? e));
            }
        }
    }

    private async Task SaveHubFile(PluginHubData cache, CancellationToken cancelToken)
    {
        try
        {
            using FileStream fs = File.Create(Path.Combine(FileUtilities.AppData, listRepoCacheFile));
            await MessagePackSerializer.SerializeAsync<PluginHubData>(fs, cache, cancellationToken: cancelToken);
            Log.Info("Saved new Plugin Hub cache");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Error while caching Plugin Hub zip file: ", ex);
        }
    }

    private async Task<PluginHubData> ReadPluginCache(CancellationToken cancelToken = default)
    {
        Log.Info("Reading Plugin Hub cache");

        string cacheFile = Path.Combine(FileUtilities.AppData, listRepoCacheFile);
        if (!File.Exists(cacheFile))
        {
            Log.Warn("Plugin Hub cache does not exist");
            return null;
        }

        try
        {
            using FileStream fs = File.OpenRead(cacheFile);
            return await MessagePackSerializer.DeserializeAsync<PluginHubData>(fs, cancellationToken: cancelToken);
        }
        catch (Exception ex)
        {
            Log.Error("Error while reading plugin data from cache: ", ex);
            return null;
        }
    }

}
