using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Interfaces;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Ana telemetry servisi - veritabanına log yazma işlemleri
    /// Thread-safe, EF Core optimized
    /// </summary>
    public sealed class TelemetryService : ITelemetryService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TelemetryService> _logger;

        public TelemetryService(AppDbContext context, ILogger<TelemetryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogApiCallAsync(string endpoint, string method, bool success, int statusCode,
            int durationMs, string category, string correlationId)
        {
            try
            {
                var log = new ApiCallLog
                {
                    Endpoint = endpoint,
                    Method = method,
                    Success = success,
                    StatusCode = statusCode,
                    DurationMs = durationMs,
                    Category = category,
                    TimestampUtc = DateTime.UtcNow,
                    CorrelationId = correlationId
                };

                _context.ApiCallLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogDebug("API call logged: {Endpoint} {Method} {StatusCode} in {Duration}ms",
                    endpoint, method, statusCode, durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log API call: {Endpoint} {Method}", endpoint, method);
                // Don't throw - telemetry failures shouldn't break the application
            }
        }

        public async Task LogCircuitStateChangeAsync(string previousState, string newState, string reason,
            double failureRate, string correlationId)
        {
            try
            {
                var log = new CircuitStateLog
                {
                    PreviousState = previousState,
                    NewState = newState,
                    Reason = reason,
                    FailureRate = failureRate,
                    TransitionTimeUtc = DateTime.UtcNow,
                    CorrelationId = correlationId
                };

                _context.CircuitStateLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Circuit state changed: {PreviousState} → {NewState} (Rate: {FailureRate:P2})",
                    previousState, newState, failureRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log circuit state change: {PreviousState} → {NewState}",
                    previousState, newState);
                // Don't throw - telemetry failures shouldn't break the application
            }
        }
    }
}
