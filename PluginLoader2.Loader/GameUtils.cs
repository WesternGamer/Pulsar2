using Keen.Game2;
using Keen.VRage.Library.Utils;
using System;
using System.Threading.Tasks;

namespace PluginLoader2.Loader;

static class GameUtils
{
    public static Version GameVersion => typeof(GameAppComponentObjectBuilder).Assembly.GetName().Version;

    public static void InvokeSync(Func<Task> task)
    {
        Task.Run(task).GetAwaiter().GetResult();
    }
    public static T InvokeSync<T>(Func<Task<T>> task)
    {
        return Task.Run(task).GetAwaiter().GetResult();
    }
}
