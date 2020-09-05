using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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

        public void copy(string srcPath, string destPath, bool handleDirectory)
        {
            try
            {
                bool isDirectory = isPathDirectory(srcPath);

                if (isDirectory)
                {
                    if (handleDirectory)
                    {
                        createDirectory(destPath);
                    }
                }
                else
                {
                    File.Copy(srcPath, destPath, true);
                }
            }
            catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
            {
                // Ignored exceptions
                // If the source no longer exists, do nothing
                Logger.log(Logger.Level.Debug, $"Ignore exception copy {srcPath} -> {destPath}");
            }
        }

        public void createDirectory(string path)
        {
            DirectoryInfo destDirectoryInfo = new DirectoryInfo(path);
            destDirectoryInfo.Create();
        }
    }
}
