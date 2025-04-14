using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PluginLoader2.Plugins;

class FileWatcher : IDisposable
{
    private static readonly TimeSpan FileChangeTimeout = TimeSpan.FromSeconds(5);

    private CancellationTokenSource cts = new CancellationTokenSource();
    private readonly FileSystemWatcher watcher;
    private readonly string directory;
    private readonly string filter;
    private readonly Timer timer;
    private bool disposed;
    private CancellationToken cancelToken;

    public delegate Task FilesChangedDelegate(string[] files, CancellationToken cancelToken);
    public event FilesChangedDelegate OnFilesChanged;

    public FileWatcher(string directory, bool subDirectories, string filter)
    {
        this.directory = directory;
        this.filter = filter;

        watcher = new FileSystemWatcher(directory, filter);
        watcher.NotifyFilter = NotifyFilters.CreationTime
                             | NotifyFilters.FileName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Security
                             | NotifyFilters.Size;

        watcher.IncludeSubdirectories = subDirectories;
        watcher.EnableRaisingEvents = false;

        timer = new Timer(TimerTriggered);
    }

    public async Task Start(CancellationToken cancelToken)
    {
        this.cancelToken = cancelToken;

        await RaiseFilesChanged();

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnChanged;
        watcher.Error += WatcherError;

        watcher.EnableRaisingEvents = true;
    }

    private void WatcherError(object sender, ErrorEventArgs e)
    {
        Log.Error(e.GetException());
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        // Wait for files that are currently being written
        timer.Change(FileChangeTimeout, Timeout.InfiniteTimeSpan);
    }

    private async void TimerTriggered(object state)
    {
        await RaiseFilesChanged();
    }

    private async Task RaiseFilesChanged()
    {
        cts.Cancel();
        cts = new CancellationTokenSource();

        var handler = OnFilesChanged;
        if (handler == null)
            return;

        string[] files = Directory.GetFiles(directory, filter);
        Delegate[] invocationList = handler.GetInvocationList();
        Task[] handlerTasks = new Task[invocationList.Length];

        for (int i = 0; i < invocationList.Length; i++)
            handlerTasks[i] = ((FilesChangedDelegate)invocationList[i])(files, cts.Token);

        await Task.WhenAll(handlerTasks);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        timer.Dispose();
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
        cts.Cancel();
    }
}
