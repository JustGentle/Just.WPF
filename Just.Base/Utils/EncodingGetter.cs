using System;
using System.IO;
using System.Text;

namespace Just.Base.Utils
{
    /// <summary> 
    /// 获取文件的编码格式 
    /// </summary> 
    public class EncodingGetter
    {
        /// <summary> 
        /// 给定文件的路径，判断文件的编码类型 
        /// </summary> 
        /// <param name="filename">文件路径</param> 
        /// <returns>文件的编码类型</returns> 
        public static Encoding GetEncoding(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Encoding r = GetEncoding(fs);
            fs.Close();
            return r;
        }

        /// <summary> 
        /// 通过给定的文件流，判断文件的编码类型 
        /// </summary> 
        /// <param name="fs">文件流</param> 
        /// <returns>文件的编码类型</returns> 
        public static Encoding GetEncoding(FileStream fs)
        {
            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            var result = GetEncoding(ss);
            r.Close();
            return result;
        }

        /// <summary>
        /// 给定字节集，判断文件的编码类型
        /// </summary>
        /// <param name="bytes">字节集</param>
        /// <returns>文件的编码类型</returns>
        public static Encoding GetEncoding(byte[] bytes)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8BOM = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM 
            Encoding result = Encoding.Default;

            if (IsStartWithBytes(bytes, UnicodeBIG))
            {
                result = Encoding.BigEndianUnicode;
            }
            else if (IsStartWithBytes(bytes, Unicode))
            {
                result = Encoding.Unicode;
            }
            else if (IsStartWithBytes(bytes, UTF8BOM) || IsUTF8Bytes(bytes))
            {
                result = Encoding.UTF8;
            }
            return result;
        }

        /// <summary>
        /// 判断字节数组是否以另一数组开头
        /// </summary>
        /// <param name="data">要判断的数组</param>
        /// <param name="start">开头数组</param>
        /// <returns></returns>
        private static bool IsStartWithBytes(byte[] data, byte[] start)
        {
            if (data.Length < start.Length) return false;
            for (int i = 0; i < start.Length; i++)
            {
                if (data[i] != start[i]) return false;
            }
            return true;
        }

        /// <summary> 
        /// 判断是否是不带 BOM 的 UTF8 格式 
        /// </summary> 
        /// <param name="data"></param> 
        /// <returns></returns> 
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1; //计算当前正分析的字符应还有的字节数 
            byte curByte; //当前分析的字节. 
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前 
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1 
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }

    }
}
