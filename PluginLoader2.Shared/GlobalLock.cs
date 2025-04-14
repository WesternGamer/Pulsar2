using System;
using System.IO;
using System.Threading;

namespace PluginLoader2;

class GlobalLock : IDisposable
{
    private readonly Mutex mutex;
    private bool mutexActive;

    public GlobalLock(string name, TimeSpan acquireTimeout)
    {
        mutex = new Mutex(true, name, out mutexActive);
        if(!mutexActive)
        {
            try
            {
                mutexActive = mutex.WaitOne(acquireTimeout);
                if (!mutexActive)
                    throw new UnauthorizedAccessException("Unable to acquire lock, file is currently in use.");
            }
            catch (AbandonedMutexException)
            { } // Abandoned probably means that the process was killed or crashed
        }
    }

    public void Dispose()
    {
        if (!mutexActive)
            return;
        mutexActive = false;
        mutex.ReleaseMutex();
    }
}