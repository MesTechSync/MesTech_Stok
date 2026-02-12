using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Interfaces;

namespace MesTechStok.Core.Services
{
    /// <summary>
    /// SQL Server Resilience için telemetry wrapper
    /// Circuit breaker state değişikliklerini yakalar
    /// </summary>
    public class SqlServerResilienceTelemetry
    {
        private readonly ITelemetryService _telemetryService;

        public SqlServerResilienceTelemetry(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        /// <summary>
        /// Circuit breaker durum değişikliğini loglar
        /// </summary>
        public async Task OnCircuitStateChange(string previousState, string newState, object? context = null)
        {
            var correlationId = Guid.NewGuid().ToString("N")[..8];
            var reason = DetermineTransitionReason(previousState, newState);
            var failureRate = ExtractFailureRate(context);

            await _telemetryService.LogCircuitStateChangeAsync(
                previousState, newState, reason, failureRate, correlationId);
        }

        /// <summary>
        /// State geçiş sebebini belirler
        /// </summary>
        private static string DetermineTransitionReason(string previousState, string newState)
        {
            return (previousState, newState) switch
            {
                ("Closed", "Open") => "High failure rate detected",
                ("Open", "HalfOpen") => "Attempting recovery",
                ("HalfOpen", "Closed") => "Recovery successful",
                ("HalfOpen", "Open") => "Recovery failed",
                _ => $"Transition from {previousState} to {newState}"
            };
        }

        /// <summary>
        /// Context'ten failure rate'i çıkarır
        /// </summary>
        private static double ExtractFailureRate(object? context)
        {
            if (context is Dictionary<string, object> dict &&
                dict.TryGetValue("FailureRate", out var rate) &&
                rate is double failureRate)
            {
                return failureRate;
            }
            return 0.0;
        }
    }
}
