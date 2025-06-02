using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar2;

internal static class FileUtilities
{
    public static string AppData { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "Pulsar2");

    public static string GameAppData { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "SpaceEngineers2");

    public static string GetHash256(string file)
    {
        using SHA256 sha = SHA256.Create();
        using FileStream fileStream = new FileStream(file, FileMode.Open);
        using BufferedStream bufferedStream = new BufferedStream(fileStream);
        return GetHash(bufferedStream, sha);
    }


    public static string GetHash(Stream input, HashAlgorithm hash)
    {
        byte[] data = hash.ComputeHash(input);
        StringBuilder sb = new StringBuilder(2 * data.Length);
        foreach (byte b in data)
            sb.AppendFormat("{0:x2}", b);
        return sb.ToString();
    }

    public static string MakeSafeString(string s)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char ch in s)
        {
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else
                sb.Append('_');
        }
        return sb.ToString();
    }
}