using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TCSynchronize
{
    class Logger
    {
        public enum Level
        {
            Debug,
            Info
        }

        private static Level logLevel = Level.Info;

        public static void initialize(string filename)
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            TextWriterTraceListener traceListener = new TextWriterTraceListener(filename);
            Trace.AutoFlush = true;
            Trace.Listeners.Add(traceListener);
        }

        public static void setLoglevel(Level level)
        {
            logLevel = level;
        }

        public static void log(Level level, string message)
        {
            if (level >= logLevel)
            {
                Trace.WriteLine(message);
            }
        }

        public static void log(Level level, Exception e)
        {
            if (level >= logLevel)
            {
                Trace.WriteLine(e);
            }
        }
    }
}
