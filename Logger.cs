using System;
using System.Diagnostics;
using System.IO;

namespace BearsAdaClock
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logDir;
        private static string _logFile;
        private static bool _initialized;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            try
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _logDir = Path.Combine(baseDir, "BearsAdaClock", "logs");
                Directory.CreateDirectory(_logDir);
                _logFile = Path.Combine(_logDir, "ada-clock.log");
                _initialized = true;
            }
            catch
            {
                // Fallback to temp
                _logDir = Path.GetTempPath();
                _logFile = Path.Combine(_logDir, "ada-clock.log");
                _initialized = true;
            }
        }

        // Expose resolved paths so the app/UI can show the exact location in use
        public static string LogDirectory { get { EnsureInitialized(); return _logDir; } }
        public static string LogFilePath { get { EnsureInitialized(); return _logFile; } }

        public static void Info(string message) => Write("INFO", message);
        public static void Warn(string message) => Write("WARN", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Error(Exception ex, string context = null)
        {
            Write("ERROR", $"{context ?? ""}{(string.IsNullOrEmpty(context) ? string.Empty : ": ")}{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }

        private static void Write(string level, string message)
        {
            EnsureInitialized();
            try
            {
                lock (_lock)
                {
                    RotateIfNeeded();
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [PID {Environment.ProcessId}] {message}";
                    File.AppendAllText(_logFile, line + Environment.NewLine);
                    Debug.WriteLine(line);
                }
            }
            catch
            {
                // ignore logging failures
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                if (File.Exists(_logFile))
                {
                    var fi = new FileInfo(_logFile);
                    if (fi.Length > 1_000_000) // ~1MB
                    {
                        string archive = Path.Combine(_logDir, $"ada-clock-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                        File.Move(_logFile, archive, true);
                    }
                }
            }
            catch { }
        }
    }
}
