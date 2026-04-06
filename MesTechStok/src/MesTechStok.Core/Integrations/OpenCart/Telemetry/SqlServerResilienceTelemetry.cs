using System;
using MesTechStok.Core.Integrations.OpenCart.Telemetry;
using MesTech.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MesTechStok.Core.Integrations.OpenCart.Telemetry
{
    /// <summary>
    /// Persists resilience + API call telemetry into SQL Server via DbContext.
    /// Uses service locator to resolve DbContext without direct Infrastructure reference.
    /// Lightweight, fire-and-forget; failures swallowed.
    /// </summary>
    internal sealed class SqlServerResilienceTelemetry : IResilienceTelemetry
    {
        private readonly IServiceProvider _sp;
        public SqlServerResilienceTelemetry(IServiceProvider sp) { _sp = sp; }

        public void OnRetry(string endpoint, string method, int attempt, TimeSpan delay, int? statusCode, string? correlationId)
        {
            // Optional: Could record retry events; skipped to reduce volume.
        }

        public void OnCircuitStateChange(CircuitStateSnapshot oldState, CircuitStateSnapshot newState, double failRate, int windowTotal, string? correlationId)
        {
            // Circuit state transition persistence - critical for operational visibility
            try
            {
                using var scope = _sp.CreateScope();
                // Resolve DbContext by its concrete type via service locator — avoids Core→Infrastructure reference
                var dbType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == "AppDbContext" && typeof(DbContext).IsAssignableFrom(t));

                if (dbType is null) return;

                var db = (DbContext)scope.ServiceProvider.GetRequiredService(dbType);
                db.Set<CircuitStateLog>().Add(new CircuitStateLog
                {
                    PreviousState = oldState.ToString(),
                    NewState = newState.ToString(),
                    Reason = DetermineTransitionReason(oldState, newState, failRate),
                    FailureRate = failRate,
                    WindowTotalCalls = windowTotal,
                    TransitionTimeUtc = DateTime.UtcNow,
                    CorrelationId = correlationId,
                    AdditionalInfo = $"FailRate:{failRate:P2},Window:{windowTotal}"
                });
                db.SaveChanges();
            }
            catch
            {
                // Circuit transition logging should not crash main flow
            }
        }

        private static string DetermineTransitionReason(CircuitStateSnapshot oldState, CircuitStateSnapshot newState, double failRate)
        {
            return (oldState, newState) switch
            {
                (CircuitStateSnapshot.Closed, CircuitStateSnapshot.Open) => $"FailureThreshold({failRate:P1})",
                (CircuitStateSnapshot.Open, CircuitStateSnapshot.HalfOpen) => "OpenTimeout",
                (CircuitStateSnapshot.HalfOpen, CircuitStateSnapshot.Closed) => "SuccessStreak",
                (CircuitStateSnapshot.HalfOpen, CircuitStateSnapshot.Open) => "HalfOpenFailure",
                _ => "Unknown"
            };
        }

        public void OnApiCall(string endpoint, string method, TimeSpan duration, bool success, int? statusCode, OpenCartErrorCategory category, string? correlationId)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var dbType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == "AppDbContext" && typeof(DbContext).IsAssignableFrom(t));

                if (dbType is null) return;

                var db = (DbContext)scope.ServiceProvider.GetRequiredService(dbType);
                db.Set<ApiCallLog>().Add(new ApiCallLog
                {
                    Endpoint = endpoint,
                    Method = method,
                    Success = success,
                    StatusCode = statusCode,
                    Category = category.ToString(),
                    DurationMs = (long)duration.TotalMilliseconds,
                    TimestampUtc = DateTime.UtcNow,
                    CorrelationId = correlationId
                });
                db.SaveChanges();
            }
            catch
            {
                // Swallow – telemetry persistence should not crash main flow.
            }
        }
    }
}
