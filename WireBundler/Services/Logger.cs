using System;
using System.IO;
using WireBundler.Services;

namespace WireBundler.Infrastructure
{
    /// <summary>
    /// Logs messages to both the console and a file.
    /// </summary>
    public class Logger
    {
        private readonly string logFilePath;

        public Logger(string logFilePath)
        {
            this.logFilePath = logFilePath;
        }

        /// <summary>
        /// Writes the log message to the console and the log file.
        /// </summary>
        public void Log(LogLevel level, string message)
        {
            string line = $"{DateTime.Now:dd-MM-yyyy HH:mm:ss} [{level}] {message}";
            Console.WriteLine(line);

            try
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logger ERROR: {ex.Message}");
            }
        }
    }
}