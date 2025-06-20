﻿using System.IO;
using System.Xml.Serialization;

namespace Pulsar2.Loader.Compile.GitHub;

public class GitHubAssetFile
{
    public enum AssetType { Asset, Lib, LibContent }

    public string Name { get; set; }
    public string Hash { get; set; }
    public long Length { get; set; }
    public AssetType Type { get; set; }
    [XmlIgnore]
    public string BaseDir { get; set; }

    public string NormalizedFileName => Name.Replace('\\', '/').TrimStart('/');

    public string FullPath => Path.GetFullPath(Path.Combine(BaseDir, Name));

    public GitHubAssetFile()
    {

    }

    public GitHubAssetFile(string file, AssetType type)
    {
        Name = file;
        Type = type;
    }

    public void GetFileInfo()
    {
        string file = FullPath;
        if (!File.Exists(file))
            return;

        FileInfo info = new FileInfo(file);
        Length = info.Length;
        Hash = FileUtilities.GetHash256(file);
    }

    public bool IsValid()
    {
        string file = FullPath;
        if (!File.Exists(file))
            return false;

        FileInfo info = new FileInfo(file);
        if (info.Length != Length)
            return false;

        string newHash = FileUtilities.GetHash256(file);
        if (newHash != Hash)
            return false;

        return true;
    }

    public void Save(Stream stream)
    {
        string newFile = FullPath;
        Directory.CreateDirectory(Path.GetDirectoryName(newFile));
        using (FileStream file = File.Create(newFile))
        {
            stream.CopyTo(file);
        }

        GetFileInfo();
    }

}