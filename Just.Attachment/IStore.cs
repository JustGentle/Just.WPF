using System;
using System.Collections.Generic;

namespace Just.Attachment
{
    public interface IStore
    {
        StoreServerConfig Config { get; }
        bool Init(StoreServerConfig config);
        bool Dispose();
        string GetFullPath(AttachmentInfo atta);
        IEnumerable<string> GetDirectories(string parent);
        IEnumerable<string> GetFiles(string folder);
        DateTime? GetWriteTime(string file);
        string GetParentDirectory(string path);
        void MoveDirectory(string source, string target);
        void DeleteDirectory(string path);
        void MoveFile(string source, string target);
        void DeleteFile(string path);
        bool DirectoryExists(string path);
        bool FileExists(string path);
        void CreateDirectory(string path);
    }
}
