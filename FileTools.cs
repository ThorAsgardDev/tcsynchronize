using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace TCSynchronize
{
    class FileTools
    {
        public bool isPathDirectory(string path)
        {
            FileAttributes fileAttributes = File.GetAttributes(path);
            return ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory);
        }

        public void rename(string oldPath, string newPath)
        {
            try
            {
                // Directory.Move works for directories AND files.

                // Check if rename is just a case letters change
                if (string.Compare(oldPath, newPath, true) != 0)
                {
                    Directory.Move(oldPath, newPath);
                }
                else
                {
                    // Rename in two phasis because Directory.Move doesn't handle rename when it's only a case letters change
                    Directory.Move(oldPath, oldPath + ".tmp");
                    Directory.Move(oldPath + ".tmp", newPath);
                }
            }
            catch (DirectoryNotFoundException e)
            {
                // Ignored exceptions
                // If the old path does not exist, do nothing
                _ = e;
                Logger.log(Logger.Level.Debug, $"Ignore exception rename {oldPath} -> {newPath}");
            }
        }

        public void remove(string path)
        {
            try
            {
                bool isDirectory = isPathDirectory(path);
                if (isDirectory)
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    File.Delete(path);
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                // Ignored exceptions
                // If the path does not exist, do nothing
                Logger.log(Logger.Level.Debug, $"Ignore exception remove {path}");
            }
        }

        public void copy(string srcPath, string destPath, bool handleDirectory, Filter filter)
        {
            try
            {
                bool isDirectory = isPathDirectory(srcPath);

                if (isDirectory)
                {
                    if (handleDirectory)
                    {
                        copyDirectory(srcPath, destPath, filter, false);
                    }
                }
                else
                {
                    copyFile(srcPath, destPath);
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                // Ignored exceptions
                // If the source no longer exists, do nothing
                Logger.log(Logger.Level.Debug, $"Ignore exception copy {srcPath} -> {destPath}");
            }
        }

        private void createDirectory(string path)
        {
            DirectoryInfo destDirectoryInfo = new DirectoryInfo(path);
            destDirectoryInfo.Create();
        }

        public void copyFile(string srcPath, string destPath)
        {
            FileAttributes attributes = File.GetAttributes(destPath);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                // Make the file RW
                attributes &= ~FileAttributes.ReadOnly;
                File.SetAttributes(destPath, attributes);
            }

            File.Copy(srcPath, destPath, true);
        }

        public void copyDirectory(string srcPath, string destPath, Filter filter, bool logsOuput)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            long nScannedEntries = 0;
            long nScannedDirectories = 0;
            long nUpdatedDirectories = 0;
            long nScannedFiles = 0;
            long nUpdatedFiles = 0;

            createDirectory(destPath);

            EnumerationOptions enumerationOptions = new EnumerationOptions();
            enumerationOptions.RecurseSubdirectories = true;
            IEnumerable<string> srcEntries = Directory.EnumerateFileSystemEntries(srcPath, "*", enumerationOptions);

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

                            if (isPathDirectory(srcEntry))
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
                                        rename(destEntry, destEntry);
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
                                    copyFile(srcEntry, destEntry);
                                }
                                else
                                {
                                    // Check if the directory name is the same (case sensitive)
                                    string name = destFileInfo.Name;
                                    string realName = destFileInfo.Directory.GetFileSystemInfos(destFileInfo.Name)[0].Name;

                                    if (string.Compare(name, realName) != 0)
                                    {
                                        nUpdatedFiles++;
                                        rename(destEntry, destEntry);
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
