using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginLoader2;

public class ProcessLaunchOptions
{
    public ProcessLaunchOptions() { }

    public ProcessLaunchOptions(string[] args)
    {
        foreach(string arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--nostart":
                    DisableAutoStart = true;
                    break;
                case "--start":
                    SkipLauncher = true;
                    break;
            }

        }
    }

    public bool DisableAutoStart { get; set; }

    public bool SkipLauncher { get; set; }
}