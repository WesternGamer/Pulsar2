using System;
using System.Xml.Serialization;

namespace Pulsar2;

public class SerializableVersion
{
    public int Major { get; set; }

    public int Minor { get; set; }

    public int Build { get; set; }

    public int Revision { get; set; }

    [XmlIgnore]
    public Version Object 
    {
        get 
        {
            if (Build == -1 && Revision == -1)
                return new Version(Major, Minor);
            if (Revision == -1)
                return new Version(Major, Minor, Build);
            return new Version(Major, Minor, Build, Revision);
        }
    }

    public SerializableVersion()
    {

    }

    public SerializableVersion(Version v)
    {
        Major = v.Major;
        Minor = v.Minor;
        Build = v.Build;
        Revision = v.Revision;
    }
}