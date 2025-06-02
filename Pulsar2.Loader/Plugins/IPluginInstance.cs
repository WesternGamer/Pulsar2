using System.Net.Http;
using System.Reflection;

namespace Pulsar2.Loader.Plugins;

interface IPluginInstance
{
    Assembly Load(); 
}
