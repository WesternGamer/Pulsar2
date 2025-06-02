using NuGet.Common;
using Serilog.Core;
using System.Threading.Tasks;

namespace Pulsar2.Loader.Compile.NuGet;

public class NuGetLogger : ILogger
{
    private readonly Logger log = Pulsar2.Log.CreateCustomLogger("NuGet");

    public void Log(LogLevel level, string data)
    {
        log.Write(ConvertLogLevel(level), data);
    }

    public void Log(ILogMessage message)
    {
        Log(message.Level, message.Message);
    }

    private static Serilog.Events.LogEventLevel ConvertLogLevel(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Debug:
                return Serilog.Events.LogEventLevel.Debug;
            case LogLevel.Verbose:
                return Serilog.Events.LogEventLevel.Debug;
            case LogLevel.Information:
                return Serilog.Events.LogEventLevel.Information;
            case LogLevel.Minimal:
                return Serilog.Events.LogEventLevel.Information;
            case LogLevel.Warning:
                return Serilog.Events.LogEventLevel.Warning;
            case LogLevel.Error:
                return Serilog.Events.LogEventLevel.Error;
            default:
                return Serilog.Events.LogEventLevel.Information;
        }
    }

    public Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
        return Task.CompletedTask;
    }

    public Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }

    public void LogDebug(string data)
    {
        Log(LogLevel.Debug, data);
    }

    public void LogError(string data)
    {
        Log(LogLevel.Error, data);
    }

    public void LogInformation(string data)
    {
        Log(LogLevel.Information, data);
    }

    public void LogInformationSummary(string data)
    {
        Log(LogLevel.Information, data);
    }

    public void LogMinimal(string data)
    {
        Log(LogLevel.Minimal, data);
    }

    public void LogVerbose(string data)
    {
        Log(LogLevel.Verbose, data);
    }

    public void LogWarning(string data)
    {
        Log(LogLevel.Warning, data);
    }
}
