using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Just
{
    public static class Logger
    {
        public const string REPOSITORY = "Just.WPF";
        public const string CONFIG = "log4net.config";
        public const string LOGGER = "FileLogger";
        public static ILog Instance { get; private set; }
        static Logger()
        {
            var logRepository = LogManager.CreateRepository(REPOSITORY);
            XmlConfigurator.Configure(logRepository, new FileInfo(CONFIG));
            Instance = LogManager.GetLogger(REPOSITORY, LOGGER);
        }
        /// <summary>
        /// 获取调用日志方法信息
        /// </summary>
        /// <returns></returns>
        /// <example>[IngoWinService.Common.Logger:GetLogMethodInfo] - </example>
        private static string GetLogMethodInfo()
        {
            var method = GetMethodStack(3);
            if (method == null)
                return string.Empty;
            return $"[{method.DeclaringType.FullName}:{method.Name}] - ";
        }

        /// <summary>
        /// 获取调用当前方法的上级方法
        /// </summary>
        /// <param name="frameIndex">层次索引</param>
        /// <returns></returns>
        private static MethodBase GetMethodStack(int frameIndex = 2)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(frameIndex)?.GetMethod();
            return method;
        }


        /// <summary>
        /// 全局调试日志纪录入口
        /// </summary>
        /// <param name="msg"></param>
        public static void Debug(string msg)
        {
            Instance.Debug(GetLogMethodInfo() + msg);
        }
        /// <summary>
        /// 全局信息日志纪录入口
        /// </summary>
        /// <param name="msg"></param>
        public static void Info(string msg)
        {
            Instance.Info(GetLogMethodInfo() + msg);
        }
        /// <summary>
        /// 全局警告日志纪录入口
        /// </summary>
        /// <param name="msg"></param>
        public static void Warn(string msg)
        {
            Instance.Warn(GetLogMethodInfo() + msg);
        }

        /// <summary>
        /// 全局异常日志纪录入口
        /// </summary>
        /// <param name="msg"></param>
        public static void Error(string msg)
        {
            Instance.Error(GetLogMethodInfo() + msg);
        }

        /// <summary>
        /// 全局异常日志纪录入口
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Error(string msg, Exception ex)
        {
            Instance.Error(GetLogMethodInfo() + msg, ex);
        }

        /// <summary>
        /// 全局致命错误日志纪录入口
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="ex"></param>
        public static void Fatal(string msg, Exception ex)
        {
            Instance.Fatal(GetLogMethodInfo() + msg, ex);
        }
    }
}
