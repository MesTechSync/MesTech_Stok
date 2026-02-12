using System;

namespace MesTechStok.Desktop.Services
{
    public interface IOpenCartHealthService
    {
        DateTime? LastSuccessUtc { get; }
        DateTime? LastFailureUtc { get; }
        int ConsecutiveFailures { get; }
        string? LastErrorMessage { get; }
        long ProcessedCount { get; }

        void OnSuccess();
        void OnFailure(string errorMessage);
    }

    public class OpenCartHealthService : IOpenCartHealthService
    {
        private readonly object _lock = new();
        public DateTime? LastSuccessUtc { get; private set; }
        public DateTime? LastFailureUtc { get; private set; }
        public int ConsecutiveFailures { get; private set; }
        public string? LastErrorMessage { get; private set; }
        public long ProcessedCount { get; private set; }

        public void OnSuccess()
        {
            lock (_lock)
            {
                LastSuccessUtc = DateTime.UtcNow;
                ConsecutiveFailures = 0;
                ProcessedCount++;
            }
        }

        public void OnFailure(string errorMessage)
        {
            lock (_lock)
            {
                LastFailureUtc = DateTime.UtcNow;
                ConsecutiveFailures++;
                LastErrorMessage = errorMessage;
            }
        }
    }
}


