using System;

namespace MesTechStok.Core.Integrations.OpenCart.Telemetry
{
    public enum CircuitStateSnapshot { Closed, Open, HalfOpen }
    public enum OpenCartErrorCategory { None, Transient, Network, Timeout, Auth, Validation, NotFound, RateLimit, Unknown }

    public interface IResilienceTelemetry
    {
        void OnRetry(string endpoint, string method, int attempt, TimeSpan delay, int? statusCode, string? correlationId);
        void OnCircuitStateChange(CircuitStateSnapshot oldState, CircuitStateSnapshot newState, double failRate, int windowTotal, string? correlationId);
        void OnApiCall(string endpoint, string method, TimeSpan duration, bool success, int? statusCode, OpenCartErrorCategory category, string? correlationId);
    }

    internal sealed class NoopResilienceTelemetry : IResilienceTelemetry
    {
        public static readonly NoopResilienceTelemetry Instance = new();
        public void OnRetry(string endpoint, string method, int attempt, TimeSpan delay, int? statusCode, string? correlationId) { }
        public void OnCircuitStateChange(CircuitStateSnapshot oldState, CircuitStateSnapshot newState, double failRate, int windowTotal, string? correlationId) { }
        public void OnApiCall(string endpoint, string method, TimeSpan duration, bool success, int? statusCode, OpenCartErrorCategory category, string? correlationId) { }
    }
}
