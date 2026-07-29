using System;
using System.Collections.Generic;
using System.Text;
using WireBundler.Infrastructure;

namespace WireBundler.Services
{
    /// <summary>
    /// Lightweight application logging facade that delegates logging to an underlying <see cref="WireBundler.Infrastructure.Logger"/>.
    /// Provides simple initialization and a centralized Write method used across the application.
    /// </summary>
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
