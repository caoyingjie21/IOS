using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IOS.Viewer.Services
{
    /// <summary>
    /// 提供日志记录功能的服务类
    /// </summary>
    public class LogService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string _logDirectory;

        /// <summary>
        /// 初始化 LogService 实例，创建日志目录
        /// </summary>
        public LogService()
        {
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QTHaasConsole",
                "logs");

            Directory.CreateDirectory(_logDirectory);
        }

        /// <summary>
        /// 获取所有日志文件路径，按文件名倒序排列
        /// </summary>
        /// <returns>日志文件路径列表</returns>
        public List<string> GetLogFiles()
        {
            return Directory.GetFiles(_logDirectory, "log_*.log")
                .OrderByDescending(f => f)
                .ToList();
        }

        /// <summary>
        /// 异步读取指定日志文件内容
        /// </summary>
        /// <param name="filePath">日志文件路径</param>
        /// <returns>日志文件内容列表</returns>
        public async Task<List<string>> ReadLogFile(string filePath)
        {
            // log4net can lock files, so we need to read it in a way that doesn't conflict.
            // A simple approach is to copy the file and read from the copy.
            var tempPath = Path.GetTempFileName();
            File.Copy(filePath, tempPath, true);
            var lines = await File.ReadAllLinesAsync(tempPath);
            File.Delete(tempPath);
            return lines.ToList();
        }

        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogInfo(string message)
        {
            log.Info(message);
        }

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogWarning(string message)
        {
            log.Warn(message);
        }

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志信息</param>
        public void LogError(string message)
        {
            log.Error(message);
        }

        /// <summary>
        /// 记录异常错误级别日志
        /// </summary>
        /// <param name="ex">异常对象</param>
        public void LogError(Exception ex)
        {
            log.Error(ex.Message, ex);
        }
    }
}
