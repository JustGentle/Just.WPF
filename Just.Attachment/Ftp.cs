using Just.Base.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Just.Attachment
{
    public class Ftp
    {
        /// <summary>
        /// 分块缓存大小
        /// </summary>
        public int BufferSize { get; set; } = 2048;

        private string _server;
        private string _username;
        private string _password;
        /// <summary>
        /// 创建Ftp操作对象
        /// </summary>
        /// <param name="server">服务器名和端口</param>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        public Ftp(string server, string username = null, string password = null)
        {
            server = server.Replace('\\', '/').TrimEnd('/');
            _server = server.StartsWith(Uri.UriSchemeFtp) ? server : $"{Uri.UriSchemeFtp}{Uri.SchemeDelimiter}{server}";
            _username = username;
            _password = password;
        }

        /// <summary>
        /// 下载文件数据
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        public byte[] Download(string path)
        {
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    using (var outputStream = new MemoryStream())
                    {
                        //分块读取
                        var buffer = new byte[BufferSize];
                        var readSize = BufferSize;
                        while (readSize == BufferSize)
                        {
                            readSize = ftpStream.Read(buffer, 0, BufferSize);
                            outputStream.Write(buffer, 0, readSize);
                        }
                        return outputStream.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 下载文件到路径
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        public void Download(string path, string save)
        {
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    if (File.Exists(save))
                        PathHelper.DeleteFileWithNormal(save);
                    using (var outputStream = new FileStream(save, FileMode.Create))
                    {
                        //分块读取
                        var buffer = new byte[BufferSize];
                        var readSize = BufferSize;
                        while (readSize == BufferSize)
                        {
                            readSize = ftpStream.Read(buffer, 0, BufferSize);
                            outputStream.Write(buffer, 0, readSize);
                        }
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">文件数据</param>
        /// <param name="append">是否追加到已有文件</param>
        public void Upload(string path, byte[] content, bool append = false)
        {
            //先创建文件夹
            MakeDirectories(Path.GetDirectoryName(GetUrlPath(path)));

            var request = CreateRequest(path);
            if (append)
                request.Method = WebRequestMethods.Ftp.AppendFile;
            else
                request.Method = WebRequestMethods.Ftp.UploadFile;

            using (var ftpStream = request.GetRequestStream())
            {
                using (var inputStream = new MemoryStream(content))
                {
                    //分块写入
                    var buffer = new byte[BufferSize];
                    var readSize = BufferSize;
                    while (readSize == BufferSize)
                    {
                        readSize = inputStream.Read(buffer, 0, BufferSize);
                        ftpStream.Write(buffer, 0, readSize);
                    }
                }
            }
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path">文件路径</param>
        public void Delete(string path)
        {
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            using (var response = request.GetResponse() as FtpWebResponse)
            {
                //不需要读取
            }
        }
        public void DeleteDirectory(string path)
        {
            path = EnsureIsDirectoryPath(path);
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.RemoveDirectory;

            using (var response = request.GetResponse() as FtpWebResponse)
            {
                //不需要读取
            }
        }

        /// <summary>
        /// 创建相对路径中所有文件夹
        /// </summary>
        /// <param name="dirPath"></param>
        public void MakeDirectories(string dirPath)
        {
            dirPath = GetUrlPath(dirPath);
            if (string.IsNullOrEmpty(dirPath))
                return;

            var parts = dirPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var current = "";
            var notExists = false;
            for (int i = 0; i < parts.Length; i++)
            {
                //文件夹
                var folder = parts[i];
                if (notExists)
                {
                    //某一层级已经不存在了,则不需要再判断
                    MakeDirectory(current + "/" + folder);
                }
                else
                {
                    var dirs = ListDirectory(current);
                    if (!dirs.Contains(folder))
                    {
                        notExists = true;
                        MakeDirectory(current + "/" + folder);
                    }
                }
                //下一个路径
                if (i > 0)
                    current += "/";
                current += "/" + folder;
            }
        }
        /// <summary>
        /// 列出当前目录内的子文件夹和文件名
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<string> ListDirectory(string path)
        {
            path = EnsureIsDirectoryPath(path);
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(ftpStream, Encoding.UTF8))
                    {
                        while (reader.Peek() > 0)
                        {
                            yield return reader.ReadLine();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 获取当前目录内的子文件夹
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<string> GetDirectories(string path)
        {
            path = EnsureIsDirectoryPath(path);
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(ftpStream, Encoding.UTF8))
                    {
                        while (reader.Peek() > 0)
                        {
                            var detail = GetDetailInfo(reader.ReadLine());
                            if (detail.IsDirectory)
                                yield return detail.Name;
                            else
                                continue;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 获取当前目录内的文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<string> GetFiles(string path)
        {
            path = EnsureIsDirectoryPath(path);
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(ftpStream, Encoding.UTF8))
                    {
                        while (reader.Peek() > 0)
                        {
                            var detail = GetDetailInfo(reader.ReadLine());
                            if (!detail.IsDirectory)
                                yield return detail.Name;
                            else
                                continue;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 列出当前目录内的子文件夹和文件信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<FtpDetail> ListDirectoryDetail(string path)
        {
            path = EnsureIsDirectoryPath(path);
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(ftpStream, Encoding.UTF8))
                    {
                        while (reader.Peek() > 0)
                        {
                            yield return GetDetailInfo(reader.ReadLine());
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 文件夹是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool DirectoryExists(string path)
        {
            var dirPath = GetUrlPath(path);
            if (dirPath == null)
                return true;

            var parts = dirPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var current = "";
            var notExists = false;
            for (int i = 0; i < parts.Length; i++)
            {
                //文件夹
                var folder = parts[i];
                var dirs = GetDirectories(current);
                if (!dirs.Contains(folder))
                {
                    notExists = true;
                    break;
                }
                //下一个路径
                if (i > 0)
                    current += "/";
                current += "/" + folder;
            }

            return !notExists;
        }
        /// <summary>
        /// 文件是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool FileExists(string path)
        {
            var dirPath = GetParentDirectoryPath(path);
            var dirExists = DirectoryExists(dirPath);
            if (!dirExists)
                return false;

            var fileName = Path.GetFileName(path);
            var files = GetFiles(dirPath);
            return files.Contains(fileName);
        }
        /// <summary>
        /// 获取文件或目录最后修改时间
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public DateTime? GetDateTime(string path)
        {
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            using (var response = request.GetResponse() as FtpWebResponse)
            {
                return response.LastModified;
            }
        }

        //相对路径取完整路径
        public string GetPathUrl(string path)
        {
            //兼容错误的斜杠方向
            path = path?.Replace('\\', '/').TrimStart('/');
            return $"{_server}/{path}";
        }
        //完整路径取相对路径
        public string GetUrlPath(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "/";
            //兼容错误的斜杠方向
            url = url.Replace('\\', '/');
            if (url.StartsWith(_server))
            {
                url = url.Remove(0, _server.Length);
                if (!url.StartsWith("/"))
                    url = "/" + url;
            }
            return url;
        }
        public string GetParentDirectoryPath(string path)
        {
            var dirPath = Path.GetDirectoryName(GetUrlPath(path))?.Replace('\\', '/');
            return EnsureIsDirectoryPath(dirPath);
        }

        //创建ftp请求
        private FtpWebRequest CreateRequest(string url)
        {
            //兼容完整路径和相对路径
            url = url.StartsWith(Uri.UriSchemeFtp) ? url : GetPathUrl(url);
            var request = WebRequest.Create(url) as FtpWebRequest;
            if (!string.IsNullOrEmpty(_username))
            {
                request.Credentials = new NetworkCredential(_username, _password);
            }
            request.KeepAlive = false;
            request.UseBinary = true;
            request.UsePassive = false;
            return request;
        }

        /// <summary>
        /// 创建文件夹（父级不存在会出错）
        /// </summary>
        /// <param name="path">文件夹路径</param>
        private void MakeDirectory(string path)
        {
            var request = CreateRequest(path);
            request.Method = WebRequestMethods.Ftp.MakeDirectory;

            using (var response = request.GetResponse())
            {
                using (var ftpStream = response.GetResponseStream())
                {
                    //不需要读取
                }
            }
        }
        //目录以斜杠结尾，有些ftp要求
        private string EnsureIsDirectoryPath(string path)
        {
            path = path ?? "/";
            if (!path.EndsWith("/"))
                path += "/";
            return path;
        }

        private readonly Regex rgxNumber = new Regex(@"^[\d\.]+$");
        private const char folderPermission = 'd';
        private const string folderToken = "<DIR>";
        /// <summary>
        /// 解析Ftp的LIST返回值，dotnet不支持MLSD命令，可能需要根据不同Ftp做个性化解析
        /// </summary>
        /// <param name="infoLine"></param>
        /// <returns></returns>
        private FtpDetail GetDetailInfo(string infoLine)
        {
            FtpDetail result;
            string[] tokens = infoLine.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
            var isMSDOS = rgxNumber.IsMatch(infoLine[0].ToString());

            if (isMSDOS)
            {
                /*
                 * MS-DOS
                 05-11-20  10:11AM       <DIR>          1
                 05-27-19  06:33PM               892966 02f91d01-b259-4168-ae46-aa6525c8680f.pdf
                 */
                result.Name = tokens[3];
                result.IsDirectory = tokens[2] == folderToken;
            }
            else
            {
                /*
                 * UNIX
                 drwxrwxrwx   1 owner    group               0 May 11 10:11 1
                 -rwxrwxrwx   1 owner    group          892966 May 27  2019 02f91d01-b259-4168-ae46-aa6525c8680f.pdf
                 */
                result.Name = tokens[8];
                var permissions = tokens[0];
                result.IsDirectory = permissions[0] == folderPermission;
            }
            return result;
        }

        /// <summary>
        /// Ftp列表信息
        /// </summary>
        public struct FtpDetail
        {
            public bool IsDirectory;
            public string Name;
        }
    }
}
