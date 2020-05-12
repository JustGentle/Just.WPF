using Just.Base.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Just.Attachment
{
    public class FtpStore : IStore
    {
        public StoreServerConfig Config { get; protected set; }
        private Ftp Ftp { get; set; }

        public bool Init(StoreServerConfig config)
        {
            Config = config;
            Ftp = new Ftp(config.Server, config.Username, config.Password);
            return true;
        }

        public bool Dispose()
        {
            Config = null;
            Ftp = null;
            return true;
        }

        public void DeleteDirectory(string path)
        {
            Ftp.DeleteDirectory(path);
        }

        public void DeleteFile(string path)
        {
            Ftp.Delete(path);
        }

        public IEnumerable<string> GetDirectories(string parent)
        {
            return Ftp.GetDirectories(parent).Select(d => $"{parent.TrimEnd('/')}/{d}");
        }

        public IEnumerable<string> GetFiles(string folder)
        {
            return Ftp.GetFiles(folder).Select(f => $"{folder.TrimEnd('/')}/{f}");
        }

        public string GetFullPath(AttachmentInfo atta)
        {
            string suffix = (string.IsNullOrEmpty(atta.Suffix) ? atta.Extension : atta.Suffix);
            if (!suffix.StartsWith("."))
            {
                suffix = "." + suffix;
            }
            string path = Path.Combine(atta.Root, atta.DirectoryPath, $"{atta.FileReName}{suffix}").Replace("\\", "/").Trim('/');
            path = $"{Uri.UriSchemeFtp}{Uri.SchemeDelimiter}{atta.Server}/{path}";
            return path;
        }

        public string GetParentDirectory(string path)
        {
            return Ftp.GetParentDirectoryPath(path);
        }

        public DateTime? GetWriteTime(string file)
        {
            return Ftp.GetDateTime(file);
        }

        public void MoveDirectory(string source, string target)
        {
            source = source.TrimEnd('/');
            target = target.TrimEnd('\\');
            //这里来源地址是Ftp文件夹,目标地址是本地文件夹
            var details = Ftp.ListDirectoryDetail(source);
            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);
            foreach (var item in details)
            {
                var sourceItem = $"{source}/{item.Name}";
                var targetItem = $"{target}\\{item.Name}";
                if (item.IsDirectory)
                {
                    MoveDirectory(sourceItem, targetItem);
                }
                else
                {
                    if (File.Exists(targetItem))
                    {
                        PathHelper.DeleteFileWithNormal(targetItem);
                    }
                    Ftp.Download(sourceItem, targetItem);
                }
            }
            if (!string.IsNullOrEmpty(source))
                Ftp.DeleteDirectory(source);
        }

        public void MoveFile(string source, string target)
        {
            var dir = Path.GetDirectoryName(target);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(target))
            {
                PathHelper.DeleteFileWithNormal(target);
            }
            Ftp.Download(source, target);
            Ftp.Delete(source);
        }

        public bool DirectoryExists(string path)
        {
            return Ftp.DirectoryExists(path);
        }

        public bool FileExists(string path)
        {
            return Ftp.FileExists(path);
        }

        public void CreateDirectory(string path)
        {
            Ftp.MakeDirectories(path);
        }
    }
}
