using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace TCSynchronize
{
    class Synchronizer
    {
        private FileTools fileTools;

        public Synchronizer()
        {
            fileTools = new FileTools();
        }

        public void synchronize(string srcPath, string destPath, Filter filter, bool logsOuput)
        {
            if (logsOuput)
            {
                Logger.log(Logger.Level.Info, $"Synchronizing {srcPath} to {destPath}...");
            }
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            EnumerationOptions enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            IEnumerable<string> srcEntries = Directory.EnumerateFileSystemEntries(srcPath, "*", enumerationOptions);

            fileTools.createDirectory(destPath);

            long nScannedEntries = 0;
            long nScannedDirectories = 0;
            long nUpdatedDirectories = 0;
            long nScannedFiles = 0;
            long nUpdatedFiles = 0;
            foreach (string srcEntry in srcEntries)
            {
                if (!filter.isPathFiltered(srcEntry))
                {
                    int nTries = 0;

                    while (true)
                    {
                        try
                        {
                            string destEntry = srcEntry.Replace(srcPath, destPath);

                            if (fileTools.isPathDirectory(srcEntry))
                            {
                                nScannedDirectories++;
                                DirectoryInfo destDirectoryInfo = new DirectoryInfo(destEntry);
                                if (!destDirectoryInfo.Exists)
                                {
                                    nUpdatedDirectories++;
                                    //Logger.log(Logger.Level.Info, srcEntry);
                                    destDirectoryInfo.Create();
                                }
                                else
                                {
                                    // Check if the directory name is the same (case sensitive)
                                    string name = destDirectoryInfo.Name;
                                    string realName = destDirectoryInfo.Parent.GetFileSystemInfos(destDirectoryInfo.Name)[0].Name;
                                    
                                    if (string.Compare(name, realName) != 0)
                                    {
                                        nUpdatedDirectories++;
                                        fileTools.rename(destEntry, destEntry);
                                    }
                                }
                            }
                            else
                            {
                                nScannedFiles++;

                                FileInfo srcFileInfo = new FileInfo(srcEntry);
                                FileInfo destFileInfo = new FileInfo(destEntry);

                                if (!destFileInfo.Exists
                                || (srcFileInfo.LastWriteTimeUtc.CompareTo(destFileInfo.LastWriteTimeUtc) == 1)
                                || (srcFileInfo.Length != destFileInfo.Length))
                                {
                                    nUpdatedFiles++;
                                    //Logger.log(Logger.Level.Info, srcEntry);
                                    File.Copy(srcEntry, destEntry, true);
                                }
                                else
                                {
                                    // Check if the directory name is the same (case sensitive)
                                    string name = destFileInfo.Name;
                                    string realName = destFileInfo.Directory.GetFileSystemInfos(destFileInfo.Name)[0].Name;

                                    if (string.Compare(name, realName) != 0)
                                    {
                                        nUpdatedFiles++;
                                        fileTools.rename(destEntry, destEntry);
                                    }
                                }
                            }
                            break;
                        }
                        catch (Exception e)
                        {
                            nTries++;

                            if (nTries >= 10)
                            {
                                Logger.log(Logger.Level.Info, $"Synchronization in error on {srcEntry} after 10 attemps.");
                                throw e;
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                    }
                }

                nScannedEntries++;
                if (logsOuput)
                {
                    if (nScannedEntries >= 1 && (nScannedEntries % 5000) == 0)
                    {
                        Logger.log(Logger.Level.Info, $"Number of scanned entries: {nScannedEntries}");
                    }
                }
            }

            stopwatch.Stop();

            if (logsOuput)
            {
                Logger.log(Logger.Level.Info, $"Synchronization completed in {stopwatch.ElapsedMilliseconds}ms");
                Logger.log(Logger.Level.Info, $"Directories: {nScannedDirectories} scanned, {nUpdatedDirectories} updated");
                Logger.log(Logger.Level.Info, $"Files: {nScannedFiles} scanned, {nUpdatedFiles} updated");
            }
        }
    }
}
