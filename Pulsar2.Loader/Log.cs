using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Serilog.Formatting;
using System.Reflection;


namespace Pulsar2;

internal class Log
{
    private const string LogFormat = "{@t:HH:mm:ss} [{@l:u3}] [{ThreadID}]{CustomPrefix} {@m} {@x}\n";
    private const string GameLogFormat = "[PluginLoader] {CustomPrefix} {@m} {@x}";

    private static Logger log;
    private static readonly List<Logger> customLoggers = new List<Logger>();


    public static void Init(string file)
    {
        ExpressionTemplate format = new ExpressionTemplate(LogFormat);
        LoggerConfiguration logConfig = new LoggerConfiguration()
            .Enrich.With(new ThreadIDEnricher())
#if DEBUG
            .WriteTo.Debug(format)
#endif
            .WriteTo.Sink(new GameLogSink(new ExpressionTemplate(GameLogFormat)))
            .WriteTo.File(format, file, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30);

        //logConfig = logConfig.WriteTo.Console(new ExpressionTemplate(LogFormat, theme: TemplateTheme.Literate));

        log = logConfig.CreateLogger();

        try
        {
            AssemblyName a = Assembly.GetExecutingAssembly().GetName();
            Info($"Starting {a.Name} - v{a.Version}");
        }
        catch { }
    }

    public static Logger CreateCustomLogger(string name)
    {
        LoggerConfiguration config = new LoggerConfiguration()
            .Enrich.With(new PrefixEnricher(" [" + name + "]"))
            .WriteTo.Logger(log);
        Logger customLog = config.CreateLogger();
        customLoggers.Add(customLog);
        return customLog;
    }

    public static void Info(string msg)
    {
        Write(LogEventLevel.Information, msg);
    }

    public static void Error(string msg)
    {
        Write(LogEventLevel.Error, msg);
    }

    public static void Warn(string msg)
    {
        Write(LogEventLevel.Warning, msg);
    }

    internal static void Warn(string msg, Exception exception)
    {
        log.Warning(exception, msg);
    }


    public static void Error(Exception ex)
    {
        log.Error(ex, "An exception was thrown:");
    }

    public static void Error(string msg, Exception ex)
    {
        log.Error(ex, msg);
    }

    public static void Write(LogEventLevel level, string msg)
    {
        log.Write(level, msg);
    }

    public static void Close()
    {
        log.Dispose();
        foreach (Logger log in customLoggers)
            log.Dispose();
    }

    private class PrefixEnricher : ILogEventEnricher
    {
        private readonly string prefix;

        public PrefixEnricher(string prefix)
        {
            this.prefix = prefix;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
              "CustomPrefix", prefix));
        }
    }

    private class ThreadIDEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
              "ThreadID", Environment.CurrentManagedThreadId.ToString()));
        }
    }


    private class GameLogSink : ILogEventSink
    {
        private readonly ITextFormatter format;

        public GameLogSink(ITextFormatter format)
        {
            this.format = format;
        }

        public void Emit(LogEvent logEvent)
        {
            StringBuilder sb = new StringBuilder();
            using (TextWriter writer = new StringWriter(sb)) 
            {
                format.Format(logEvent, writer);
            }
            Keen.VRage.Library.Diagnostics.Log.Default.WriteLine(GetSeverity(logEvent.Level), sb);
        }

        private Keen.VRage.Library.Diagnostics.LogSeverity GetSeverity(LogEventLevel level) 
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    return Keen.VRage.Library.Diagnostics.LogSeverity.Debug;
                case LogEventLevel.Debug:
                    return Keen.VRage.Library.Diagnostics.LogSeverity.Debug;
                case LogEventLevel.Information:
                    return Keen.VRage.Library.Diagnostics.LogSeverity.Info;
                case LogEventLevel.Warning:
                    return Keen.VRage.Library.Diagnostics.LogSeverity.Warning;
                case LogEventLevel.Error:
                    return Keen.VRage.Library.Diagnostics.LogSeverity.Error;
                case LogEventLevel.Fatal:
                    return Keen.VRage.Library.Diagnostics.LogSeverity.Critical;
            }
            return Keen.VRage.Library.Diagnostics.LogSeverity.Info;
        }
    }
}
