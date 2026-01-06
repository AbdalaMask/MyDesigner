using System;

namespace MyDesigner.Design.Services.Integration
{
    /// <summary>
    /// Thread-local context for logging during file-opening operations.
    /// This allows code in the designer host (Document, Shell, etc.) to send log messages
    /// back to the VS Code extension without having direct access to the TCP writer.
    /// </summary>
    public static class FileOpeningLogContext
    {
        [ThreadStatic]
        private static Action<string, string>? _currentLogger;

        /// <summary>
        /// Sets the logger action for the current thread.
        /// </summary>
        /// <param name="logger">Action that receives (level, message) pairs</param>
        public static void SetLogger(Action<string, string>? logger)
        {
            _currentLogger = logger;
        }

        /// <summary>
        /// Gets the current logger, or null if not set.
        /// </summary>
        public static Action<string, string>? GetLogger() => _currentLogger;

        /// <summary>
        /// Clears the logger for the current thread.
        /// </summary>
        public static void ClearLogger()
        {
            _currentLogger = null;
        }

        /// <summary>
        /// Logs a message if a logger is available.
        /// </summary>
        public static void Log(string level, string message)
        {
            _currentLogger?.Invoke(level, message);
            System.Diagnostics.Debug.WriteLine($"[{level.ToUpper()}] {message}");
        }

        public static void Info(string message) => Log("info", message);
        public static void Warn(string message) => Log("warn", message);
        public static void Error(string message) => Log("error", message);
        public static void Debug(string message) => Log("debug", message);
    }
}
