using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginLoader2.Launch;

internal interface IGameLauncher
{
    bool IsValid { get; }

    int Priority { get; }

    bool CheckIsRunning();

    Task<bool> StartGame();
}