using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Templates;

namespace PluginLoader2;

internal static class Log
{
    private const string LogFormat = "{@t:HH:mm:ss} [{@l:u3}] [{ThreadID}]{CustomPrefix} {@m} {@x}\n";

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
            .WriteTo.File(format, file, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30);

        //logConfig = logConfig.WriteTo.Console(new ExpressionTemplate(LogFormat, theme: TemplateTheme.Literate));

        log = logConfig.CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
                log.Fatal(ex, "Unhandled exception:");
            else
                log.Fatal("An unhandled exception occurred");
        }
        catch { }
    }

    public static void Link(ILoggingBuilder logBuilder, string name)
    {
        logBuilder.AddSerilog(CreateCustomLogger(name), false);
    }
    public static void Link(ILoggerFactory logBuilder, string name)
    {
        logBuilder.AddSerilog(CreateCustomLogger(name), false);
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

    private static void Write(LogEventLevel level, string msg)
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

}