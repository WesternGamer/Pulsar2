using Avalonia.Platform;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Packaging;
using PluginLoader2.Config;
using PluginLoader2.Loader.Compile;
using PluginLoader2.Loader.Compile.GitHub;
using PluginLoader2.Loader.Compile.NuGet;
using PluginLoader2.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Branch = PluginLoader2.Plugins.GitHubPluginBranch;

namespace PluginLoader2.Loader.Plugins;

class GitHubPlugin : IPluginInstance
{
    private readonly GitHubPluginData data;
    private readonly HttpClient web;
    private readonly GitHubPluginConfig config;
    private AssemblyResolver resolver;

    public GitHubPlugin(GitHubPluginData data, HttpClient web, GitHubPluginConfig config)
    {
        this.data = data;
        this.web = web;
        this.config = config;
    }

    private Branch GetBranch()
    {
        if(!string.IsNullOrEmpty(config.Branch))
        {
            Branch selectedBranch = data.Versions.FirstOrDefault(x => x.Id == config.Branch);
            if (selectedBranch != null)
            {
                Log.Info($"Using branch {config.Branch} for {data.Name}");
                return selectedBranch;
            }
        }
        return data.Versions[0];
    }

    public Assembly Load()
    {
        resolver = new AssemblyResolver();
        GitHubCacheManifest manifest = GitHubCacheManifest.Load(data.Username, data.Repository);

        Branch branch = GetBranch();
        if (branch.NuGetReferences == null)
            branch.NuGetReferences = [];
        
        Version gameVersion = GameUtils.GameVersion;

        if (!manifest.IsCacheValid(branch.Commit, gameVersion, !string.IsNullOrWhiteSpace(branch.AssetFolder), branch.NuGetReferences.Length > 0))
        {
            manifest.GameVersion = new SerializableVersion(gameVersion);
            manifest.Commit = branch.Commit;
            manifest.ClearAssets();
            string name = FileUtilities.MakeSafeString(this.data.Id) + '_' + Path.GetRandomFileName();

            byte[] data = CompileFromSource(name, manifest, branch);
            File.WriteAllBytes(manifest.DllFile, data);
            manifest.DeleteUnknownFiles();
            manifest.Save();

            resolver.AddSourceFolder(manifest.LibDir);
            resolver.AddAllowedAssemblyFile(manifest.DllFile);
            resolver.AddAllowedAssemblyName(name);
            return Assembly.Load(data);
        }
        else
        {
            manifest.DeleteUnknownFiles();
            resolver.AddSourceFolder(manifest.LibDir);
            resolver.AddAllowedAssemblyFile(manifest.DllFile);
            return Assembly.LoadFile(manifest.DllFile);
        }
    }

    private Stream DownloadRepo(string commit)
    {
        Uri uri = new Uri($"https://github.com/{data.Username}/{data.Repository}/archive/{commit}.zip", UriKind.Absolute);
        MemoryStream output = new MemoryStream();
        using Stream body = GameUtils.InvokeSync(() => web.GetStreamAsync(uri));
        body.CopyTo(output);
        output.Position = 0;
        return output;
    }

    public byte[] CompileFromSource(string assemblyName, GitHubCacheManifest manifest, Branch branch)
    {
        RoslynCompiler compiler = new RoslynCompiler();

        using (Stream s = DownloadRepo(branch.Commit))
        using (ZipArchive zip = new ZipArchive(s))
        {
            for (int i = 0; i < zip.Entries.Count; i++)
            {
                ZipArchiveEntry entry = zip.Entries[i];
                AddSource(compiler, entry, manifest, branch);
            }
        }

        using MemoryStream mem = new MemoryStream();
        compiler.Compile(assemblyName, GetReferences(branch, manifest), mem);
        return mem.ToArray();
    }

    private void AddSource(RoslynCompiler compiler, ZipArchiveEntry entry, GitHubCacheManifest manifest, Branch branch)
    {
        string path = RemoveRoot(entry.FullName);

        if (AllowedZipPath(path, branch.SourceDirectories))
        {
            using (Stream entryStream = entry.Open())
                compiler.AddSource(entryStream, entry.FullName);
        }
        if (IsAssetZipPath(path, branch.AssetFolder, out string assetFilePath))
        {
            GitHubAssetFile newFile = manifest.CreateAsset(assetFilePath);
            if (!manifest.IsAssetValid(newFile))
            {
                using (Stream entryStream = entry.Open())
                    manifest.SaveAsset(newFile, entryStream);
            }
        }
    }

    private bool IsAssetZipPath(string path, string assetFolder, out string assetFilePath)
    {
        assetFilePath = null;

        if (path.EndsWith('/') || string.IsNullOrEmpty(assetFolder))
            return false;

        assetFolder = assetFolder.Replace('\\', '/').TrimStart('/');

        if (path.StartsWith(assetFolder, StringComparison.Ordinal) && path.Length > (assetFolder.Length + 1))
        {
            assetFilePath = path.Substring(assetFolder.Length).TrimStart('/');
            return true;
        }
        return false;
    }

    private bool AllowedZipPath(string path, string[] sourceDirectories)
    {
        if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (sourceDirectories == null || sourceDirectories.Length == 0)
            return true;

        foreach (string dir in sourceDirectories)
        {
            if (path.StartsWith(dir.Replace('\\', '/').TrimStart('/'), StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private string RemoveRoot(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        int index = path.IndexOf('/');
        if (index >= 0 && (index + 1) < path.Length)
            return path.Substring(index + 1);
        return path;
    }


    private CompilerReferences GetReferences(Branch branch, GitHubCacheManifest manifest)
    {
        CompilerReferences references = new CompilerReferences();

        if (branch.NuGetReferences.Length > 0)
        {
            NuGetClient nuget = new NuGetClient();
            foreach (NuGetPackage package in nuget.DownloadPackages(branch.NuGetReferences))
                InstallPackage(package, references, manifest);
        }

        return references;
    }

    private void InstallPackage(NuGetPackage package, CompilerReferences references, GitHubCacheManifest manifest)
    {
        foreach (NuGetPackage.Item file in package.LibFiles)
        {
            GitHubAssetFile newFile = manifest.CreateAsset(file.FilePath, GitHubAssetFile.AssetType.Lib);
            if (!manifest.IsAssetValid(newFile))
            {
                using (Stream entryStream = File.OpenRead(file.FullPath))
                    manifest.SaveAsset(newFile, entryStream);
            }

            if (Path.GetDirectoryName(newFile.FullPath) == newFile.BaseDir)
                references.TryAddDependency(newFile.FullPath);
        }

        foreach (NuGetPackage.Item file in package.ContentFiles)
        {
            GitHubAssetFile newFile = manifest.CreateAsset(file.FilePath, GitHubAssetFile.AssetType.LibContent);
            if (!manifest.IsAssetValid(newFile))
            {
                using (Stream entryStream = File.OpenRead(file.FullPath))
                    manifest.SaveAsset(newFile, entryStream);
            }
        }
    }
}
