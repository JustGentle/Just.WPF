using System;
using System.IO;

namespace Just.Base.Utils
{
    public static class Dialog
    {
        public static string GetInitialDirectory(string path)
        {
            try
            {
                return Path.GetDirectoryName(path);
            }
            catch (Exception ex)
            {
                Logger.Error("获取初始文件夹错误", ex);
                return string.Empty;
            }
        }
    }
}
