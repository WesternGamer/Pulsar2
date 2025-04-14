using System;
using System.IO;
using System.Xml.Serialization;

namespace PluginLoader2.Config;

public abstract class ConfigFile
{
    private string filePath;

    protected virtual void Init()
    {

    }

    internal static bool TryLoad<T>(string path, out T config) where T : ConfigFile, new()
    {
        if (!File.Exists(path))
        {
            config = new T();
            config.filePath = path;
            config.Init();
            config.Save();
            return true;
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                config = (T)serializer.Deserialize(fs);
            config.filePath = path;
            config.Init();
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred while loading config: ", e);
            config = null;
            return false;
        }

    }

    internal void Save()
    {

        try
        {
            Log.Info("Saving config");
            XmlSerializer serializer = new XmlSerializer(GetType());
            using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            serializer.Serialize(fs, this);
        }
        catch (Exception e)
        {
            Log.Error($"An error occurred while saving config: ", e);
        }
    }
}