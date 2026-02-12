using System;

namespace MesTechStok.Desktop.Views
{
    // Shim to preserve existing references to Views.GlobalLogger while delegating to Utils.GlobalLogger
    public static class GlobalLogger
    {
        public static MesTechStok.Desktop.Utils.GlobalLogger Instance => MesTechStok.Desktop.Utils.GlobalLogger.Instance;

        public static void LogInfo(string message, string source = "General") => Instance.LogInfo(message, source);
        public static void LogWarning(string message, string source = "General") => Instance.LogWarning(message, source);
        public static void LogError(string message, string source = "General") => Instance.LogError(message, source);
        public static void LogEvent(string eventType, string message, string source = "General") => Instance.LogEvent(eventType, message, source);
        public static void LogAudit(string eventType, string message, string source = "Audit") => Instance.LogAudit(eventType, message, source);
    }
}
