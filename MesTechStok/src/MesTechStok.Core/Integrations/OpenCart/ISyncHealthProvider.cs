using System;

namespace MesTechStok.Core.Integrations.OpenCart
{
    public interface ISyncHealthProvider
    {
        DateTime? LastSuccessUtc { get; }
        DateTime? LastFailureUtc { get; }
        int ConsecutiveFailures { get; }
        string? LastCorrelationId { get; }
        string? LastErrorCategory { get; }
        string? CircuitState { get; }
        void MarkSuccess(string? correlationId);
        void MarkFailure(string? correlationId, string? category = null);
        void UpdateCircuitState(string state);
    }

    internal class InMemorySyncHealthProvider : ISyncHealthProvider
    {
        private readonly object _lock = new();
        public DateTime? LastSuccessUtc { get; private set; }
        public DateTime? LastFailureUtc { get; private set; }
        public int ConsecutiveFailures { get; private set; }
        public string? LastCorrelationId { get; private set; }
        public string? LastErrorCategory { get; private set; }
        public string? CircuitState { get; private set; }

        public void MarkSuccess(string? correlationId)
        {
            lock (_lock)
            {
                LastSuccessUtc = DateTime.UtcNow;
                LastCorrelationId = correlationId;
                ConsecutiveFailures = 0;
            }
        }

        public void MarkFailure(string? correlationId, string? category = null)
        {
            lock (_lock)
            {
                LastFailureUtc = DateTime.UtcNow;
                LastCorrelationId = correlationId;
                ConsecutiveFailures++;
                LastErrorCategory = category;
            }
        }
        public void UpdateCircuitState(string state)
        {
            lock (_lock)
            {
                CircuitState = state;
            }
        }
    }
}
