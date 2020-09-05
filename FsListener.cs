using System.IO;
using System.Collections.Concurrent;

namespace TCSynchronize
{
    class FsListener
    {
        private FileSystemWatcher fileSystemWatcher;
        private SynchronizationParameters synchronizationParameters;
        private BlockingCollection<FsEvent> eventList;

        public FsListener(SynchronizationParameters synchronizationParameters, BlockingCollection<FsEvent> eventList)
        {
            this.synchronizationParameters = synchronizationParameters;
            this.eventList = eventList;

            fileSystemWatcher = new FileSystemWatcher();

            fileSystemWatcher.Path = synchronizationParameters.getSrcPath();

            // Watch for changes LastWrite times, and
            // the renaming of files or directories.
            fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite 
                                           | NotifyFilters.FileName 
                                           | NotifyFilters.DirectoryName;

            fileSystemWatcher.InternalBufferSize = 64 * 1024;
            fileSystemWatcher.IncludeSubdirectories = true;

            // Add event handlers.
            fileSystemWatcher.Changed += onChanged;
            fileSystemWatcher.Created += onChanged;
            fileSystemWatcher.Deleted += onChanged;
            fileSystemWatcher.Renamed += onRenamed;
            fileSystemWatcher.Error += onError;

            // Begin watching.
            fileSystemWatcher.EnableRaisingEvents = true;

            Logger.log(Logger.Level.Info, $"Listening for changes in {synchronizationParameters.getSrcPath()}...");
        }

        private void onChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted
            && (File.Exists(e.FullPath) || Directory.Exists(e.FullPath)))
            {
                // Ignore this case, it's a delete event sent before a rename 
                // from oldname to newname with oldname == newname (ignoring case)
                // and oldname != newname (case sensitive)
            }
            else
            {
                FsEvent fsEvent = new FsEvent(synchronizationParameters);
                fsEvent.fileSystemEventArgs = e;
                eventList.Add(fsEvent);
            }
        }

        private void onRenamed(object source, RenamedEventArgs e)
        {
            FsEvent fsEvent = new FsEvent(synchronizationParameters);
            fsEvent.renamedEventArgs = e;
            eventList.Add(fsEvent);
        }

        private void onError(object source, ErrorEventArgs e)
        {
            FsEvent fsEvent = new FsEvent(synchronizationParameters);
            fsEvent.errorEventArgs = e;
            eventList.Add(fsEvent);
        }
    }
}
