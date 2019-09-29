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
        private static System.Security.Cryptography.MD5 _MD5 = System.Security.Cryptography.MD5.Create();
        public static string GetFileHash(string file)
        {
            var hash = BitConverter.ToString(_MD5.ComputeHash(File.ReadAllBytes(file)));
            return hash;
        }
    }
}
