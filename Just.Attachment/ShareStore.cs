using Just.Base;
using System;
using System.IO;

namespace Just.Attachment
{
    public class ShareStore : LocalStore
    {
        public override bool Init(StoreServerConfig config)
        {
            if (!base.Init(config))
                return false;
            Config = config;
            return ConnectShare($@"\\{config.Server}", config.Username, config.Password, config.Domain);
        }
        public override bool Dispose()
        {
            if (!DisConnectShare($@"\\{Config.Server}"))
                return false;
            return base.Dispose();
        }
        public override string GetFullPath(AttachmentInfo atta)
        {
            string suffix = (string.IsNullOrEmpty(atta.Suffix) ? atta.Extension : atta.Suffix);
            if (!suffix.StartsWith("."))
            {
                suffix = "." + suffix;
            }
            string server = $@"\\{atta.Server}";
            string root = atta.Root.Replace("/", "\\").Trim('\\');
            string path = Path.Combine(server, root, atta.DirectoryPath, $"{atta.FileReName}{suffix}").Replace("/", "\\");
            return path;
        }

        /// <summary>
        /// 连接远程共享
        /// </summary>
        /// <param name="remotePath">共享目录</param>
        /// <param name="userName">用户名</param>
        /// <param name="passWord">密码</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool ConnectShare(string remotePath, string userName, string passWord, string domain = "")
        {
            bool flag = true;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            try
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                if (!string.IsNullOrEmpty(domain))
                {
                    userName = $"{domain}\\{userName}";
                }

                string delCmd = @"%SystemRoot%\System32\net use *  /del /y";
                string conCmd = string.Format(@"%SystemRoot%\System32\net use {0}  /user:{1} {2}", remotePath, userName, passWord);
                proc.StandardInput.WriteLine(delCmd);
                proc.StandardInput.WriteLine(conCmd);
                proc.StandardInput.WriteLine("exit");
                while ((proc.HasExited == false))
                {
                    proc.WaitForExit(1000);
                }
                string errormsg = proc.StandardError.ReadToEnd();
                if (errormsg != string.Empty)
                {
                    throw new Exception("连接远程共享目录失败：" + errormsg);
                }
                proc.StandardError.Close();
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2146233088)
                {
                    //连接远程共享目录失败：发生系统错误 1219。不允许一个用户使用一个以上用户名与服务器或共享资源的多重连接。中断与此服务器或共享资源的所有连接，然后再试一次。
                    Logger.Warn("远程共享目录重复连接");
                    MainWindowVM.NotifyWarn("远程共享目录重复连接：" + remotePath, "连接共享目录");
                    return true;
                }
                Logger.Error("连接远程共享目录失败", ex);
                MainWindowVM.NotifyError("连接远程共享目录失败：" + remotePath, "连接共享目录");
                flag = false;
            }
            finally
            {
                try
                {
                    proc.Close();
                    proc.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error("关闭远程共享目录连接进程", ex);
                }
            }
            return flag;
        }
        /// <summary>
        /// 断开远程共享连接
        /// </summary>
        /// <param name="remoteHost">共享目录</param>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool DisConnectShare(string remoteHost)
        {
            bool flag = true;
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            try
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();

                remoteHost = remoteHost.Substring(0, remoteHost.Length);
                string dosLine = string.Format(@"%SystemRoot%\System32\net use {0} /del", remoteHost);
                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                while ((proc.HasExited == false))
                {
                    proc.WaitForExit(1000);
                }
                string errorMsg = proc.StandardError.ReadToEnd();
                if (errorMsg != string.Empty)
                {
                    throw new Exception("断开远程共享目录失败：" + errorMsg);
                }

                proc.StandardError.Close();
            }
            catch (Exception ex)
            {
                Logger.Error("断开远程共享目录失败", ex);
                flag = false;
            }
            finally
            {
                try
                {
                    proc.Close();
                    proc.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error("断开远程共享目录失败", ex);
                }
            }
            return flag;
        }
    }
}
