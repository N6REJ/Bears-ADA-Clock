using System;
using System.IO;

namespace BearsAdaClock
{
    /// <summary>
    /// Provides application-wide logging functionality with configurable enable/disable support.
    /// Logs are written to %LocalAppData%\N6REJ\BearsAdaClock\logs\application.log
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly object LogLock = new object();

        static Logger()
        {
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "N6REJ",
                "BearsAdaClock",
                "logs"
            );
            LogFilePath = Path.Combine(LogDirectory, "application.log");
        }

        /// <summary>
        /// Gets whether logging is currently enabled based on application settings.
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                try
                {
                    return Properties.Settings.Default.EnableLogging;
                }
                catch
                {
                    // If settings can't be read, default to disabled
                    return false;
                }
            }
        }

        /// <summary>
        /// Logs an informational message if logging is enabled.
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void Info(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// Logs a warning message if logging is enabled.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public static void Warning(string message)
        {
            Log("WARN", message);
        }

        /// <summary>
        /// Logs an error message if logging is enabled.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <param name="ex">Optional exception to include in the log</param>
        public static void Error(string message, Exception? ex = null)
        {
            string fullMessage = message;
            if (ex != null)
            {
                fullMessage += $" | Exception: {ex.GetType().Name}: {ex.Message}";
                if (ex.StackTrace != null)
                {
                    fullMessage += $" | StackTrace: {ex.StackTrace.Replace(Environment.NewLine, " | ")}";
                }
            }
            Log("ERROR", fullMessage);
        }

        /// <summary>
        /// Core logging method that writes to the log file if logging is enabled.
        /// </summary>
        /// <param name="level">Log level (INFO, WARN, ERROR)</param>
        /// <param name="message">The message to log</param>
        private static void Log(string level, string message)
        {
            if (!IsEnabled)
                return;

            try
            {
                lock (LogLock)
                {
                    // Ensure log directory exists
                    Directory.CreateDirectory(LogDirectory);

                    // Format: 2024-01-15 14:30:45.123 [INFO] Message
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"{timestamp} [{level}] {message}{Environment.NewLine}";

                    // Append to log file
                    File.AppendAllText(LogFilePath, logEntry);

                    // Also write to debug output for development
                    System.Diagnostics.Debug.WriteLine($"[{level}] {message}");
                }
            }
            catch
            {
                // Silently fail if logging fails - we don't want logging issues to crash the app
            }
        }

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        /// <returns>Full path to the application log file</returns>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// Gets the path to the log directory.
        /// </summary>
        /// <returns>Full path to the log directory</returns>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Clears the current log file.
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                lock (LogLock)
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
