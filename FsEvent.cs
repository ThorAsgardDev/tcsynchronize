using System;
using System.IO;

namespace TCSynchronize
{
    class FsEvent
    {
        private SynchronizationParameters synchronizationParameters;

        public long timeStamp { get; set; }

        public FileSystemEventArgs fileSystemEventArgs { get; set; }
        public RenamedEventArgs renamedEventArgs { get; set; }
        public ErrorEventArgs errorEventArgs { get; set; }
        public bool poison { get; set; }

        public FsEvent(SynchronizationParameters synchronizationParameters)
        {
            this.synchronizationParameters = synchronizationParameters;
            timeStamp = Environment.TickCount64;
        }

        public SynchronizationParameters getSynchronizationParameters()
        {
            return synchronizationParameters;
        }
    }
}