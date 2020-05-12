using Just.Base.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Just.Attachment
{
    public class LocalStore : IStore
    {
        public StoreServerConfig Config { get; protected set; }

        public virtual bool Init(StoreServerConfig config)
        {
            Config = config;
            return true;
        }

        public virtual bool Dispose()
        {
            Config = null;
            return true;
        }

        public void DeleteDirectory(string path)
        {
            PathHelper.DeleteDirectoryWithNormal(path);
        }

        public void DeleteFile(string path)
        {
            PathHelper.DeleteFileWithNormal(path);
        }

        public IEnumerable<string> GetDirectories(string parent)
        {
            return Directory.GetDirectories(parent);
        }

        public IEnumerable<string> GetFiles(string folder)
        {
            return Directory.GetFiles(folder);
        }

        public virtual string GetFullPath(AttachmentInfo atta)
        {
            string suffix = (string.IsNullOrEmpty(atta.Suffix) ? atta.Extension : atta.Suffix);
            if (!suffix.StartsWith("."))
            {
                suffix = "." + suffix;
            }
            string path = Path.Combine(atta.Root, atta.DirectoryPath, $"{atta.FileReName}{suffix}");
            return path;
        }

        public virtual string GetParentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            return Path.GetDirectoryName(path);
        }

        public DateTime? GetWriteTime(string file)
        {
            return File.GetLastWriteTime(file);
        }

        public void MoveDirectory(string source, string target)
        {
            PathHelper.MoveDirectory(source, target);
        }

        public void MoveFile(string source, string target)
        {
            var dir = Path.GetDirectoryName(target);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(target))
            {
                PathHelper.DeleteFileWithNormal(target);
            }
            File.Move(source, target);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}
