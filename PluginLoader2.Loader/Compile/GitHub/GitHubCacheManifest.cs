using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PluginLoader2.Loader.Compile.GitHub;

class GitHubCacheManifest
{
    private const string pluginFile = "plugin.dll";
    private const string manifestFile = "manifest.xml";
    private const string assetFolder = "Assets";
    private const string libFolder = "Bin";

    private string cacheDir;
    private string assetDir;
    private string libDir;
    private Dictionary<string, GitHubAssetFile> assetFiles = new Dictionary<string, GitHubAssetFile>();

    [XmlIgnore]
    public string DllFile { get; private set; }
    public string AssetFolder => assetDir;
    public string LibDir => libDir;

    public string Commit { get; set; }
    public SerializableVersion GameVersion { get; set; }

    [XmlArray]
    [XmlArrayItem("File")]
    public GitHubAssetFile[] AssetFiles 
    {
        get 
        {
            return assetFiles.Values.ToArray();
        }
        set 
        {
            assetFiles = value.ToDictionary(GetAssetKey);
        }
    }

    public GitHubCacheManifest()
    {

    }

    private void Init(string cacheDir)
    {
        this.cacheDir = cacheDir;
        assetDir = Path.Combine(cacheDir, assetFolder);
        libDir = Path.Combine(cacheDir, libFolder);
        DllFile = Path.Combine(cacheDir, pluginFile);

        foreach (GitHubAssetFile file in assetFiles.Values)
            SetBaseDir(file);
    }

    public static GitHubCacheManifest Load(string userName, string repoName)
    {
        string cacheDir = Path.Combine(FileUtilities.AppData, "github", FileUtilities.MakeSafeString(userName), FileUtilities.MakeSafeString(repoName));
        Directory.CreateDirectory(cacheDir);

        GitHubCacheManifest manifest;

        string manifestLocation = Path.Combine(cacheDir, manifestFile);
        if (!File.Exists(manifestLocation))
        {
            manifest = new GitHubCacheManifest();
        }
        else
        {
            XmlSerializer serializer = new XmlSerializer(typeof(GitHubCacheManifest));
            try
            {
                using (Stream file = File.OpenRead(manifestLocation))
                    manifest = (GitHubCacheManifest)serializer.Deserialize(file);
            }
            catch (Exception e)
            {
                Log.Info("Error while loading manifest file: " + e);
                manifest = new GitHubCacheManifest();
            }
        }

        manifest.Init(cacheDir);
        return manifest;
    }

    public bool IsCacheValid(string currentCommit, Version currentGameVersion, bool requiresAssets, bool requiresPackages)
    {
        if (!File.Exists(DllFile) || Commit != currentCommit)
            return false;

        if (currentGameVersion != null)
        {
            Version storedVersion = GameVersion?.Object;
            if (storedVersion == null || storedVersion != currentGameVersion)
                return false;
        }

        if (requiresAssets && !assetFiles.Values.Any(x => x.Type == GitHubAssetFile.AssetType.Asset))
            return false;

        if (requiresPackages && !assetFiles.Values.Any(x => x.Type != GitHubAssetFile.AssetType.Asset))
            return false;

        foreach (GitHubAssetFile file in assetFiles.Values)
        {
            if (!file.IsValid())
                return false;
        }

        return true;
    }

    public void ClearAssets()
    {
        assetFiles.Clear();
    }

    public GitHubAssetFile CreateAsset(string file, GitHubAssetFile.AssetType type = GitHubAssetFile.AssetType.Asset)
    {
        file = file.Replace('\\', '/').TrimStart('/');
        GitHubAssetFile asset = new GitHubAssetFile(file, type);
        SetBaseDir(asset);
        asset.GetFileInfo();
        assetFiles[GetAssetKey(asset)] = asset;
        return asset;
    }

    private string GetAssetKey(GitHubAssetFile asset)
    {
        if (asset.Type == GitHubAssetFile.AssetType.Asset)
            return assetFolder + "/" + asset.NormalizedFileName;
        return libFolder + "/" + asset.NormalizedFileName;
    }

    private void SetBaseDir(GitHubAssetFile asset)
    {
        asset.BaseDir = asset.Type == GitHubAssetFile.AssetType.Asset ? assetDir : libDir;
    }

    public bool IsAssetValid(GitHubAssetFile asset)
    {
        return asset.IsValid();
    }

    public void SaveAsset(GitHubAssetFile asset, Stream stream)
    {
        asset.Save(stream);
    }

    public void Save()
    {
        string manifestLocation = Path.Combine(cacheDir, manifestFile);
        XmlSerializer serializer = new XmlSerializer(typeof(GitHubCacheManifest));
        try
        {
            using (Stream file = File.Create(manifestLocation))
                serializer.Serialize(file, this);
        }
        catch (Exception e)
        {
            Log.Info("Error while saving manifest file: " + e);
        }
    }

    public void DeleteUnknownFiles()
    {
        DeleteUnknownFiles(assetDir);
        DeleteUnknownFiles(libDir);
    }

    public void DeleteUnknownFiles(string assetDir)
    {
        if (!Directory.Exists(assetDir))
            return;

        foreach (string file in Directory.EnumerateFiles(assetDir, "*", SearchOption.AllDirectories))
        {
            string relativePath = file.Substring(cacheDir.Length).Replace('\\', '/').TrimStart('/');
            if (!assetFiles.ContainsKey(relativePath))
                File.Delete(file);
        }
    }

    public void Invalidate()
    {
        Commit = null;
        Save();
    }
}
