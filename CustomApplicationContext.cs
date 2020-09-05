using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace TCSynchronize
{
    public class CustomApplicationContext : ApplicationContext, IStatusListener
    {
        private Icon greenIcon;
        private Icon yellowIcon;
        private Icon redIcon;
        private NotifyIcon trayIcon;
        private WindowsFormsSynchronizationContext windowsFormsSynchronizationContext;
        private bool isInError;
        private List<SynchronizationParameters> synchronizationParametersList;
        private FsListener fsListener;
        private EventProcessor eventProcessor;

        public CustomApplicationContext()
        {
            try
            {
                byte[] jsonConfigurationUtf8Bytes = File.ReadAllBytes("configuration\\configuration.json");
                Configuration configuration = JsonSerializer.Deserialize<Configuration>(jsonConfigurationUtf8Bytes);

                bool configurationIsValid = true;

                foreach (Configuration.Synchronization synchronization in configuration.synchronizations)
                {
                    if (string.IsNullOrEmpty(synchronization.srcPath))
                    {
                        Logger.log(Logger.Level.Info, "A srcPath is null or empty. Please edit configuration.json to set a valid path.");
                        configurationIsValid = false;
                        break;
                    }
                    if (string.IsNullOrEmpty(synchronization.destPath))
                    {
                        Logger.log(Logger.Level.Info, "A destPath is null or empty. Please edit configuration.json to set a valid path.");
                        configurationIsValid = false;
                        break;
                    }
                }

                if (!configurationIsValid)
                {
                    throw new Exception("Invalid configuration");
                }

                if (!string.IsNullOrEmpty(configuration.logLevel))
                {
                    Logger.setLoglevel((Logger.Level)Enum.Parse(typeof(Logger.Level), configuration.logLevel, true));
                }

                isInError = false;

                greenIcon = Icon.ExtractAssociatedIcon("resources\\green.ico");
                yellowIcon = Icon.ExtractAssociatedIcon("resources\\yellow.ico");
                redIcon = Icon.ExtractAssociatedIcon("resources\\red.ico");

                windowsFormsSynchronizationContext = new WindowsFormsSynchronizationContext();

                // Initialize Tray Icon
                trayIcon = new NotifyIcon();
                trayIcon.Icon = yellowIcon;
                trayIcon.Text = "";
                trayIcon.ContextMenuStrip = new ContextMenuStrip();
                trayIcon.ContextMenuStrip.Items.Add("Show logs", null, onMenuShowLogs);
                trayIcon.ContextMenuStrip.Items.Add("-");
                trayIcon.ContextMenuStrip.Items.Add("Exit", null, onMenuExit);

                trayIcon.Visible = true;

                synchronizationParametersList = new List<SynchronizationParameters>();
                foreach (Configuration.Synchronization synchronization in configuration.synchronizations)
                {
                    Filter filter = null;

                    if (synchronization.filterPatterns == null)
                    {
                        filter = new Filter(synchronization.srcPath, configuration.globalFilterPatterns);
                    }
                    else
                    {
                        filter = new Filter(synchronization.srcPath, synchronization.filterPatterns);
                    }
                    
                    SynchronizationParameters synchronizationParameters = new SynchronizationParameters(
                        synchronization.srcPath,
                        synchronization.destPath,
                        filter);

                    synchronizationParametersList.Add(synchronizationParameters);
                }

                Synchronizer synchronizer = new Synchronizer();
                onSynchronizing();
                foreach (SynchronizationParameters synchronizationParameters in synchronizationParametersList)
                {
                    synchronizer.synchronize(synchronizationParameters.getSrcPath(), synchronizationParameters.getDestPath(), synchronizationParameters.getFilter(), true);
                }
                BlockingCollection<FsEvent> eventList = new BlockingCollection<FsEvent>();
                foreach (SynchronizationParameters synchronizationParameters in synchronizationParametersList)
                {
                    fsListener = new FsListener(synchronizationParameters, eventList);
                }
                eventProcessor = new EventProcessor(eventList, synchronizer, this);
            }
            catch (Exception e)
            {
                clean();
                throw e;
            }
        }

        public void onError()
        {
            windowsFormsSynchronizationContext.Post(callback =>
            {
                if (trayIcon != null)
                {
                    trayIcon.Icon = redIcon;
                    trayIcon.Text = "TCSynchronize: An error occured. See logs for more info.";
                }
            }, null);
        }

        public void onListening()
        {
            windowsFormsSynchronizationContext.Post(callback =>
            {
                if (!isInError)
                {
                    if (trayIcon != null)
                    {
                        trayIcon.Icon = greenIcon;
                        trayIcon.Text = "TCSynchronize: Listening...";
                    }
                }
            }, null);
        }

        public void onSynchronizing()
        {
            windowsFormsSynchronizationContext.Post(callback =>
            {
                if (!isInError)
                {
                    if (trayIcon != null)
                    {
                        trayIcon.Icon = yellowIcon;
                        trayIcon.Text = "TCSynchronize: Synchronizing...";
                    }
                }
            }, null);
        }

        private void onMenuShowLogs(object sender, EventArgs e)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "log.txt";
            processStartInfo.UseShellExecute = true;
            Process.Start(processStartInfo);
        }

        private void onMenuExit(object sender, EventArgs e)
        {
            clean();
            Application.Exit();
        }

        private void clean()
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon = null;
            }

            if (eventProcessor != null)
            {
                eventProcessor.stop();
            }
        }

        /*private void generateConfigurationFile(string filename)
        {
            Configuration configuration = new Configuration();
            configuration.logLevel = "Info";
            configuration.synchronizations = new List<Configuration.Synchronization>();

            Configuration.Synchronization synchronization;

            synchronization = new Configuration.Synchronization();
            synchronization.srcPath = "src0";
            synchronization.destPath = "dest0";
            synchronization.filterPatterns = new List<string>();
            synchronization.filterPatterns.Add("specificPattern");
            configuration.synchronizations.Add(synchronization);

            synchronization = new Configuration.Synchronization();
            synchronization.srcPath = "src1";
            synchronization.destPath = "dest1";
            configuration.synchronizations.Add(synchronization);

            configuration.globalFilterPatterns = new List<string>();
            configuration.globalFilterPatterns.Add("globalPattern");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            byte[] b = JsonSerializer.SerializeToUtf8Bytes(configuration, options);
            File.WriteAllBytes(filename, b);
        }*/
    }
}
