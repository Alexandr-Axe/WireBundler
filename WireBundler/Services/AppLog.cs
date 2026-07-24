using System;
using System.Collections.Generic;
using System.Text;
using WireBundler.Infrastructure;

namespace WireBundler.Services
{
    public static class AppLog
    {
        private static Logger? _logger;

        public static void Initialize(string logFilePath)
        {
            _logger = new Logger(logFilePath);
        }
        public static void Write(LogLevel level, string message)
        {
            _logger?.Log(level, message);
        }
    }
}
