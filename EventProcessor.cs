using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;

namespace TCSynchronize
{
    class EventProcessor
    {
        private Thread thread;

        private BlockingCollection<FsEvent> eventList;
        private Synchronizer synchronizer;
        private IStatusListener statusListener;
        private FileTools fileTools;
        private volatile byte stopRequired;

        public EventProcessor(BlockingCollection<FsEvent> eventList, Synchronizer synchronizer, IStatusListener statusListener)
        {
            this.eventList = eventList;
            this.synchronizer = synchronizer;
            this.statusListener = statusListener;

            fileTools = new FileTools();

            stopRequired = 0;
            thread = new Thread(run);
            thread.Start();
        }

        private void run()
        {
            try
            {
                bool isInError = false;
                bool isSynchronizing = false;

                while (stopRequired == 0)
                {
                    if (eventList.Count == 0)
                    {
                        isSynchronizing = false;
                        // Wait a little to let the user see the previous status
                        Thread.Sleep(300);

                        if (!isInError)
                        {
                            statusListener.onListening();
                        }
                    }

                    FsEvent fsEvent = eventList.Take();

                    if (!isSynchronizing)
                    {
                        isSynchronizing = true;

                        if (!isInError)
                        {
                            statusListener.onSynchronizing();
                        }
                    }

                    if (!fsEvent.poison)
                    {
                        long diff = Math.Abs(Environment.TickCount64 - fsEvent.timeStamp);
                        if (diff < 1000)
                        {
                            Thread.Sleep((int)(1000 - diff));
                        }

                        int nTries = 0;

                        while (true)
                        {
                            try
                            {
                                processEvent(fsEvent);
                                break;
                            }
                            catch (Exception e)
                            {
                                nTries++;

                                if (nTries >= 10)
                                {
                                    Logger.log(Logger.Level.Info, "Process event in error after 10 attemps.");
                                    Logger.log(Logger.Level.Info, e);
                                    isInError = true;
                                    statusListener.onError();
                                    break;
                                }
                                else
                                {
                                    Logger.log(Logger.Level.Debug, $"Retry {nTries}");
                                    Thread.Sleep(500);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.log(Logger.Level.Info, e);
            }
        }

        public void stop()
        {
            stopRequired = 1;

            FsEvent fsEvent = new FsEvent(null);
            fsEvent.poison = true;
            eventList.Add(fsEvent);

            thread.Join();
        }

        private void processEvent(FsEvent fsEvent)
        {
            SynchronizationParameters synchronizationParameters = fsEvent.getSynchronizationParameters();
            string srcPath = synchronizationParameters.getSrcPath();
            string destPath = synchronizationParameters.getDestPath();
            Filter filter = synchronizationParameters.getFilter();

            if (fsEvent.fileSystemEventArgs != null)
            {
                string srcEntry = fsEvent.fileSystemEventArgs.FullPath;

                if (!filter.isPathFiltered(srcEntry))
                {
                    string destEntry = srcEntry.Replace(srcPath, destPath);

                    Logger.log(Logger.Level.Debug, $"Event {fsEvent.fileSystemEventArgs.ChangeType} fileSystemEventArgs {srcEntry}");

                    switch (fsEvent.fileSystemEventArgs.ChangeType)
                    {
                        case WatcherChangeTypes.Deleted:
                            {
                                fileTools.remove(destEntry);
                                break;
                            }

                        case WatcherChangeTypes.Created:
                            {
                                fileTools.copy(srcEntry, destEntry, true, filter);
                                break;
                            }

                        case WatcherChangeTypes.Changed:
                            {
                                fileTools.copy(srcEntry, destEntry, false, filter);
                                break;
                            }
                    }
                }
                //Logger.log(Logger.Level.Info, $"File: {fsEvent.fileSystemEventArgs.FullPath} {fsEvent.fileSystemEventArgs.ChangeType}");
            }
            else if (fsEvent.renamedEventArgs != null)
            {
                string srcOldEntry = fsEvent.renamedEventArgs.OldFullPath;
                string srcNewEntry = fsEvent.renamedEventArgs.FullPath;

                bool oldIsFiltered = filter.isPathFiltered(srcOldEntry);
                bool newIsFiltered = filter.isPathFiltered(srcNewEntry);

                Logger.log(Logger.Level.Debug, $"Event renamedEventArgs {srcOldEntry} -> {srcNewEntry}");

                if (!oldIsFiltered && !newIsFiltered)
                {
                    string destOldEntry = srcOldEntry.Replace(srcPath, destPath);
                    string destNewEntry = srcNewEntry.Replace(srcPath, destPath);
                    
                    fileTools.rename(destOldEntry, destNewEntry);
                }
                else if (oldIsFiltered && !newIsFiltered)
                {
                    string destNewEntry = srcNewEntry.Replace(srcPath, destPath);

                    bool doNextStep = true;
                    bool isDirectory = false;

                    try
                    {
                        isDirectory = fileTools.isPathDirectory(srcNewEntry);
                    }
                    catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
                    {
                        // The source no longer exists, do nothing
                        doNextStep = false;
                    }

                    if (doNextStep)
                    {
                        if (isDirectory)
                        {
                            synchronizer.synchronize(srcNewEntry, destNewEntry, filter, false);
                        }
                        else
                        {
                            try
                            {
                                fileTools.copyFile(srcNewEntry, destNewEntry);
                            }
                            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
                            {
                                // The source no longer exists, do nothing
                            }
                        }
                    }
                }
                else if (!oldIsFiltered && newIsFiltered)
                {
                    string destOldEntry = srcOldEntry.Replace(srcPath, destPath);
                    string destNewEntry = srcNewEntry.Replace(srcPath, destPath);

                    // Rename first because the src can be deleted before this point
                    // All next operations can be done using only the dest file/directory
                    fileTools.rename(destOldEntry, destNewEntry);
                    fileTools.remove(destNewEntry);
                }
                //Logger.log(Logger.Level.Info, $"File: {fsEvent.renamedEventArgs.OldFullPath} renamed to {fsEvent.renamedEventArgs.FullPath}");
            }
            else if (fsEvent.errorEventArgs != null)
            {
                //  Show that an error has been detected.
                Logger.log(Logger.Level.Info, $"The FileSystemWatcher has detected an error: {fsEvent.errorEventArgs.GetException().Message}");
                //  Give more information if the error is due to an internal buffer overflow.
                if (fsEvent.errorEventArgs.GetException().GetType() == typeof(InternalBufferOverflowException))
                {
                    //  This can happen if Windows is reporting many file system events quickly
                    //  and internal buffer of the  FileSystemWatcher is not large enough to handle this
                    //  rate of events. The InternalBufferOverflowException error informs the application
                    //  that some of the file system events are being lost.
                    Logger.log(Logger.Level.Info, $"The file system watcher experienced an internal buffer overflow: {fsEvent.errorEventArgs.GetException().Message}");
                }
            }
            else
            {
                Logger.log(Logger.Level.Info, "Unknown FsEvent type");
            }
        }
    }
}
