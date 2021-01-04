using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Just.Base.Crypto
{
    public class MD5
    {
        public static string GetFileHash(string file)
        {
            var hash = GetDataHash(File.ReadAllBytes(file));
            return hash;
        }
        public static string GetTextHash(string text)
        {
            var hash = GetDataHash(Encoding.UTF8.GetBytes(text));
            return hash;
        }
        public static string GetTextHash(string text, Encoding encoding)
        {
            var hash = GetDataHash(encoding.GetBytes(text));
            return hash;
        }
        public static string GetDataHash(byte[] data)
        {
            var _MD5 = System.Security.Cryptography.MD5.Create();
            var hash = BitConverter.ToString(_MD5.ComputeHash(data));
            return hash;
        }
    }
}
