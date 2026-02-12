using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MesTechStok.Core.Diagnostics;

namespace MesTechStok.Core.Integrations.OpenCart.Http
{
    /// <summary>
    /// DelegatingHandler applying exponential backoff retry, circuit breaker, and correlation header enrichment.
    /// Telemetry hooks (OnRetry & OnCircuitStateChange) are emitted via IResilienceTelemetry (no-op by default).
    /// </summary>
    internal class RetryAndCorrelationHandler : DelegatingHandler
    {
        private readonly int[] _backoffSeconds;
        private readonly Random _rng = new();
        private readonly double _jitterRatio;
        private readonly double _failRateThreshold;
        private readonly int _slidingWindowSeconds;
        private readonly int _openStateDurationSeconds;
        private readonly int _halfOpenMaxCalls;
        private readonly int _minimumThroughput;
        private readonly Telemetry.IResilienceTelemetry _telemetry;
        private readonly ISyncHealthProvider? _health; // optional health metrics bridge
        private readonly object _lock = new();

        private CircuitState _state = CircuitState.Closed;
        private DateTime _windowStartUtc = DateTime.UtcNow;
        private int _windowFailCount = 0;
        private int _windowTotalCount = 0;
        private DateTime _openUntilUtc = DateTime.MinValue;
        private int _halfOpenAttemptCount = 0;

        private enum CircuitState { Closed, Open, HalfOpen }

        public RetryAndCorrelationHandler(HttpMessageHandler? inner = null, int[]? backoffSeconds = null, double jitterRatio = 0.15,
                double failRateThreshold = 0.5, int slidingWindowSeconds = 60, int openStateDurationSeconds = 120, int halfOpenMaxCalls = 5, int minimumThroughput = 10,
                Telemetry.IResilienceTelemetry? telemetry = null, ISyncHealthProvider? health = null)
                : base(inner ?? new HttpClientHandler())
        {
            _backoffSeconds = (backoffSeconds == null || backoffSeconds.Length == 0) ? new[] { 1, 2, 4, 8, 16 } : backoffSeconds;
            _jitterRatio = Math.Clamp(jitterRatio, 0, 0.5);
            _failRateThreshold = failRateThreshold;
            _slidingWindowSeconds = slidingWindowSeconds;
            _openStateDurationSeconds = openStateDurationSeconds;
            _halfOpenMaxCalls = halfOpenMaxCalls;
            _minimumThroughput = minimumThroughput;
            _telemetry = telemetry ?? Telemetry.NoopResilienceTelemetry.Instance;
            _health = health;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.Headers.Contains("X-Correlation-ID"))
            {
                request.Headers.TryAddWithoutValidation("X-Correlation-ID", CorrelationContext.CurrentId);
            }

            if (IsCircuitOpen(out var openEx))
            {
                _telemetry.OnCircuitStateChange(Telemetry.CircuitStateSnapshot.Open, Telemetry.CircuitStateSnapshot.Open, 1.0, 0, CorrelationContext.CurrentId);
                throw openEx;
            }

            HttpResponseMessage? lastResponse = null;
            Exception? lastException = null;

            for (var attempt = 0; attempt <= _backoffSeconds.Length; attempt++)
            {
                lastException = null;
                try
                {
                    var cloned = CloneIfNeeded(request, attempt);
                    var response = await base.SendAsync(cloned, cancellationToken).ConfigureAwait(false);
                    RegisterOutcome(response.IsSuccessStatusCode);
                    if (!ShouldRetry(response.StatusCode, attempt))
                    {
                        return response;
                    }
                    lastResponse = response;
                    var delayNext = attempt == _backoffSeconds.Length ? TimeSpan.Zero : ComputeDelay(attempt);
                    _telemetry.OnRetry(request.RequestUri?.AbsolutePath ?? string.Empty, request.Method.Method, attempt + 1, delayNext, (int)response.StatusCode, CorrelationContext.CurrentId);
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    lastException = ex;
                    RegisterOutcome(false);
                    var delayNext = attempt == _backoffSeconds.Length ? TimeSpan.Zero : ComputeDelay(attempt);
                    _telemetry.OnRetry(request.RequestUri?.AbsolutePath ?? string.Empty, request.Method.Method, attempt + 1, delayNext, null, CorrelationContext.CurrentId);
                }

                if (attempt == _backoffSeconds.Length) break;
                try
                {
                    await Task.Delay(ComputeDelay(attempt), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
            }

            if (lastResponse != null) return lastResponse;
            if (lastException != null) throw lastException;
            throw new HttpRequestException("Request failed with unknown error and no response.");
        }

        private bool ShouldRetry(HttpStatusCode statusCode, int attempt)
        {
            if (attempt >= _backoffSeconds.Length) return false;
            return statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.TooManyRequests
                || ((int)statusCode >= 500 && (int)statusCode != 501 && (int)statusCode != 505);
        }

        private bool IsTransient(Exception ex) => ex is HttpRequestException || ex is TaskCanceledException || ex is OperationCanceledException;

        private TimeSpan ComputeDelay(int attempt)
        {
            var baseSeconds = _backoffSeconds[Math.Min(attempt, _backoffSeconds.Length - 1)];
            var jitter = baseSeconds * _jitterRatio * _rng.NextDouble();
            return TimeSpan.FromSeconds(baseSeconds + jitter);
        }

        private HttpRequestMessage CloneIfNeeded(HttpRequestMessage original, int attempt)
        {
            if (attempt == 0) return original;
            var clone = new HttpRequestMessage(original.Method, original.RequestUri);
            foreach (var h in original.Headers)
                clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
            if (original.Content != null)
            {
                var bytes = original.Content.ReadAsByteArrayAsync().Result; // small payload assumption
                var newContent = new ByteArrayContent(bytes);
                foreach (var h in original.Content.Headers)
                    newContent.Headers.TryAddWithoutValidation(h.Key, h.Value);
                clone.Content = newContent;
            }
            if (!clone.Headers.Contains("X-Correlation-ID"))
                clone.Headers.TryAddWithoutValidation("X-Correlation-ID", CorrelationContext.CurrentId);
            return clone;
        }

        #region CircuitBreakerLogic
        private bool IsCircuitOpen(out Exception ex)
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open)
                {
                    if (DateTime.UtcNow >= _openUntilUtc)
                    {
                        // move to half-open with neutral telemetry metrics
                        TransitionTo(CircuitState.HalfOpen, "open_to_halfopen_timeout", 0, 0, _state);
                        ex = null!;
                        return false; // allow a trial call
                    }
                    ex = new HttpRequestException("CircuitBreaker: OPEN (short-circuited)");
                    return true;
                }
            }
            ex = null!;
            return false;
        }

        private void RegisterOutcome(bool success)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                if ((now - _windowStartUtc).TotalSeconds > _slidingWindowSeconds)
                {
                    _windowStartUtc = now;
                    _windowFailCount = 0;
                    _windowTotalCount = 0;
                }

                _windowTotalCount++;
                if (!success) _windowFailCount++;

                var oldState = _state;
                var failRateSnapshot = _windowTotalCount == 0 ? 0d : (double)_windowFailCount / _windowTotalCount;
                switch (_state)
                {
                    case CircuitState.Closed:
                        if (_windowTotalCount >= _minimumThroughput)
                        {
                            var failRate = failRateSnapshot;
                            if (failRate >= _failRateThreshold)
                            {
                                _openUntilUtc = now.AddSeconds(_openStateDurationSeconds);
                                TransitionTo(CircuitState.Open, "fail_rate_threshold_exceeded", failRateSnapshot, _windowTotalCount, oldState);
                            }
                        }
                        break;
                    case CircuitState.HalfOpen:
                        _halfOpenAttemptCount++;
                        if (!success)
                        {
                            _openUntilUtc = now.AddSeconds(_openStateDurationSeconds);
                            TransitionTo(CircuitState.Open, "halfopen_failure", failRateSnapshot, _windowTotalCount, oldState);
                        }
                        else if (_halfOpenAttemptCount >= _halfOpenMaxCalls)
                        {
                            // Enough successes -> close
                            _windowStartUtc = now;
                            _windowFailCount = 0;
                            _windowTotalCount = 0;
                            TransitionTo(CircuitState.Closed, "halfopen_success_streak", 0, 0, oldState);
                        }
                        break;
                }
            }
        }

        private void TransitionTo(CircuitState newState, string reason, double failRate, int windowTotal, CircuitState oldState)
        {
            if (_state == newState) return;
            var prev = _state;
            _state = newState;
            _telemetry.OnCircuitStateChange(
                prev switch
                {
                    CircuitState.Open => Telemetry.CircuitStateSnapshot.Open,
                    CircuitState.HalfOpen => Telemetry.CircuitStateSnapshot.HalfOpen,
                    _ => Telemetry.CircuitStateSnapshot.Closed
                },
                newState switch
                {
                    CircuitState.Open => Telemetry.CircuitStateSnapshot.Open,
                    CircuitState.HalfOpen => Telemetry.CircuitStateSnapshot.HalfOpen,
                    _ => Telemetry.CircuitStateSnapshot.Closed
                },
                failRate,
                windowTotal,
                CorrelationContext.CurrentId);
            // Health provider surface (string state)
            // health provider removed; no-op
        }
        #endregion
    }
}
