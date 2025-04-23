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

namespace PluginLoader2.Plugins.List;

class PluginHubList
{
    public const string listRepoHash = "plugins.sha";
    public const string listRepoCacheFile = "pluginhub.bin";

    private const string repoZipUrl = "https://github.com/{0}/archive/{1}.zip";
    private const string rawUrl = "https://raw.githubusercontent.com/{0}/{1}/";
    private const string pluginHub = "https://github.com/sepluginloader/PluginHub2/archive/main.zip";

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
        PluginHubData cache = await ReadPluginCache(cancelToken);

        Log.Info("Checking " + pluginHub);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, pluginHub);

        if (cache != null && cache.HasValidTag())
            request.Headers.IfNoneMatch.Add(cache.Tag.Header);

        using HttpResponseMessage response = await SendRequest(request, cancelToken);

        if (response == null || response.StatusCode == HttpStatusCode.NotModified)
        {
            if (cache == null)
                Log.Error("Failed to load Plugin Hub data");
            else
                Log.Info("Using cached Plugin Hub content");
            return cache;
        }

        EntityTagHeaderValue etagHeader = response.Headers.ETag;
        if (etagHeader == null || string.IsNullOrEmpty(etagHeader.Tag))
        {
            Log.Warn("GitHub Etag is null");
        }
        else
        {
            if (etagHeader.IsWeak)
                Log.Warn("GitHub provided a weak etag header");
        }

        Log.Info("Downloading Plugin Hub zip file");

        try
        {
            cache = await ReadHubFile(response, cancelToken);
            if(etagHeader != null && !string.IsNullOrEmpty(etagHeader.Tag))
                cache.Tag = new PluginHubData.Hash(etagHeader);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Error while downloading Plugin Hub zip file: ", ex);
            return null;
        }

        try
        {
            await SaveHubFile(cache, cancelToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Error while caching Plugin Hub zip file: ", ex);
        }
        return cache;
    }

    private async Task<PluginHubData> ReadHubFile(HttpResponseMessage response, CancellationToken cancelToken = default)
    {
        using Stream zipFileStream = await response.Content.ReadAsStreamAsync(cancelToken);

        List<GitHubPluginData> plugins = new List<GitHubPluginData>();

        using (ZipArchive zipFile = new ZipArchive(zipFileStream))
        {
            XmlSerializer xml = new XmlSerializer(typeof(GitHubPluginData));
            foreach (ZipArchiveEntry entry in zipFile.Entries)
            {
                if (!entry.FullName.EndsWith("xml", StringComparison.OrdinalIgnoreCase))
                    continue;

                using Stream entryStream = entry.Open();
                using StreamReader entryReader = new StreamReader(entryStream);
                try
                {
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

        return new PluginHubData()
        {
            GitHubPlugins = plugins.ToArray()
        };
    }

    private async Task SaveHubFile(PluginHubData cache, CancellationToken cancelToken)
    {
        using FileStream fs = File.Create(Path.Combine(FileUtilities.AppData, listRepoCacheFile));
        await MessagePackSerializer.SerializeAsync<PluginHubData>(fs, cache, cancellationToken: cancelToken);
    }

    private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, CancellationToken cancelToken = default)
    {
        try
        {
            HttpResponseMessage response = await web.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotModified)
                return response;

            Log.Error("Error while downloading plugin data from hub: Response code: " + response.StatusCode);
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Error while downloading plugin data from hub: " + ex);
            return null;
        }
    }

    private async Task<PluginHubData> ReadPluginCache(CancellationToken cancelToken = default)
    {
        Log.Info("Reading Plugin Hub cache");

        string cacheFile = Path.Combine(FileUtilities.AppData, listRepoCacheFile);
        if (!File.Exists(cacheFile))
        {
            Log.Info("Plugin Hub cache does not exist");
            return null;
        }

        try
        {
            using FileStream fs = File.OpenRead(cacheFile);
            return await MessagePackSerializer.DeserializeAsync<PluginHubData>(fs, cancellationToken: cancelToken);
        }
        catch (Exception ex)
        {
            Log.Error("Error while reading plugin data from cache: " + ex);
            return null;
        }
    }

    private Stream GetStream(Uri uri)
    {
        Log.Info("Downloading " + uri);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
        using HttpResponseMessage response = web.Send(request);
        MemoryStream output = new MemoryStream();
        Stream responseContent = response.Content.ReadAsStream();
        responseContent.CopyTo(output);
        output.Position = 0;
        return output;
    }
    private async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancelToken = default)
    {
        Log.Info("Downloading " + uri);
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
        using HttpResponseMessage response = await web.SendAsync(request, cancelToken);
        MemoryStream output = new MemoryStream();
        Stream responseContent = await response.Content.ReadAsStreamAsync(cancelToken);
        await responseContent.CopyToAsync(output, cancelToken);
        output.Position = 0;
        return output;
    }

    private Stream DownloadRepo(string name, string commit)
    {
        Uri uri = new Uri(string.Format(repoZipUrl, name, commit), UriKind.Absolute);
        return GetStream(uri);
    }
    private async Task<Stream> DownloadRepoAsync(string name, string commit, CancellationToken cancelToken = default)
    {
        Uri uri = new Uri(string.Format(repoZipUrl, name, commit), UriKind.Absolute);
        return await GetStreamAsync(uri, cancelToken);
    }

    private Stream DownloadFile(string name, string commit, string path)
    {
        Uri uri = new Uri(string.Format(rawUrl, name, commit) + path.TrimStart('/'), UriKind.Absolute);
        return GetStream(uri);
    }
    private async Task<Stream> DownloadFileAsync (string name, string commit, string path, CancellationToken cancelToken = default)
    {
        Uri uri = new Uri(string.Format(rawUrl, name, commit) + path.TrimStart('/'), UriKind.Absolute);
        return await GetStreamAsync(uri, cancelToken);
    }
}
