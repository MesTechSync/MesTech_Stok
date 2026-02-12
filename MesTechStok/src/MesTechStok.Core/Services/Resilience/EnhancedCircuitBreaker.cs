using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesTechStok.Core.Services.Resilience
{
    /// <summary>
    /// Circuit Breaker durumları
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,    // Normal çalışma - istekler geçer
        Open,      // Arızalı durum - istekler bloke
        HalfOpen   // Test durumu - sınırlı istek geçer
    }

    /// <summary>
    /// Circuit Breaker ayarları
    /// </summary>
    public class CircuitBreakerSettings
    {
        public string Name { get; set; } = string.Empty;
        public int FailureThreshold { get; set; } = 5; // 5 başarısız istek sonrası aç
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30); // 30 saniye bekle
        public int SuccessThreshold { get; set; } = 3; // 3 başarılı istek sonrası kapat
        public TimeSpan SamplePeriod { get; set; } = TimeSpan.FromMinutes(1); // 1 dakikalık örnek
        public double FailurePercentageThreshold { get; set; } = 50.0; // %50 hata oranı
        public int MinimumThroughput { get; set; } = 10; // Minimum istek sayısı
        public bool EnableHealthCheck { get; set; } = true;
        public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
        public bool EnableFallback { get; set; } = true;
        public bool EnableMetrics { get; set; } = true;
    }

    /// <summary>
    /// Circuit Breaker metrikleri
    /// </summary>
    public class CircuitBreakerMetrics
    {
        public string Name { get; set; } = string.Empty;
        public CircuitBreakerState State { get; set; }
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public long RejectedRequests { get; set; }
        public double FailurePercentage => TotalRequests > 0 ? (double)FailedRequests / TotalRequests * 100 : 0;
        public DateTime LastStateChange { get; set; }
        public DateTime? LastFailure { get; set; }
        public DateTime? LastSuccess { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public Dictionary<string, object> CustomMetrics { get; set; } = new();
    }

    /// <summary>
    /// Circuit Breaker event verileri
    /// </summary>
    public class CircuitBreakerEventArgs : EventArgs
    {
        public string Name { get; set; } = string.Empty;
        public CircuitBreakerState OldState { get; set; }
        public CircuitBreakerState NewState { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
        public Exception? Exception { get; set; }
        public CircuitBreakerMetrics Metrics { get; set; } = new();
    }

    /// <summary>
    /// Circuit Breaker exception
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public string CircuitBreakerName { get; }
        public CircuitBreakerMetrics Metrics { get; }

        public CircuitBreakerOpenException(string name, CircuitBreakerMetrics metrics)
            : base($"Circuit breaker '{name}' is open. Failure rate: {metrics.FailurePercentage:F1}%")
        {
            CircuitBreakerName = name;
            Metrics = metrics;
        }
    }

    /// <summary>
    /// Enhanced Circuit Breaker interface
    /// </summary>
    public interface IEnhancedCircuitBreaker
    {
        string Name { get; }
        CircuitBreakerState State { get; }
        CircuitBreakerMetrics GetMetrics();

        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, Func<Task<T>>? fallback = null, CancellationToken cancellationToken = default);

        void Reset();
        void Trip(string? reason = null);
        bool IsHealthy();

        event EventHandler<CircuitBreakerEventArgs>? StateChanged;
    }

    /// <summary>
    /// Request tracking için model
    /// </summary>
    internal class RequestRecord
    {
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorType { get; set; }
    }

    /// <summary>
    /// Enhanced Circuit Breaker implementasyonu
    /// Gelişmiş özellikler: metric collection, health check, fallback, sliding window
    /// </summary>
    public class EnhancedCircuitBreaker : IEnhancedCircuitBreaker
    {
        private readonly CircuitBreakerSettings _settings;
        private readonly ILogger<EnhancedCircuitBreaker> _logger;

        private volatile CircuitBreakerState _state = CircuitBreakerState.Closed;
        private DateTime _lastStateChange = DateTime.UtcNow;
        private readonly object _stateLock = new object();

        // Metrics
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _rejectedRequests;
        private DateTime? _lastFailure;
        private DateTime? _lastSuccess;
        private readonly ConcurrentQueue<RequestRecord> _requestWindow = new();
        private readonly List<TimeSpan> _responseTimes = new();
        private readonly object _metricsLock = new object();

        // Half-open state için
        private int _halfOpenSuccessCount;
        private readonly SemaphoreSlim _halfOpenSemaphore = new(1, 1);

        public string Name { get; }
        public CircuitBreakerState State => _state;

        public event EventHandler<CircuitBreakerEventArgs>? StateChanged;

        public EnhancedCircuitBreaker(string name, CircuitBreakerSettings settings, ILogger<EnhancedCircuitBreaker> logger)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("[CircuitBreaker] Initialized '{Name}' with settings: " +
                "FailureThreshold={FailureThreshold}, Timeout={Timeout}, SuccessThreshold={SuccessThreshold}",
                Name, _settings.FailureThreshold, _settings.Timeout, _settings.SuccessThreshold);
        }

        /// <summary>
        /// Operation execute eder (fallback olmadan)
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            return await ExecuteAsync(operation, fallback: null, cancellationToken);
        }

        /// <summary>
        /// Operation execute eder (fallback ile)
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, Func<Task<T>>? fallback = null, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // State kontrolü
                await CheckAndUpdateStateAsync();

                // Open state - request reject
                if (_state == CircuitBreakerState.Open)
                {
                    Interlocked.Increment(ref _rejectedRequests);

                    if (fallback != null && _settings.EnableFallback)
                    {
                        _logger.LogWarning("[CircuitBreaker] '{Name}' is open, executing fallback. CorrelationId: {CorrelationId}",
                            Name, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);
                        return await fallback();
                    }

                    var metrics = GetMetrics();
                    _logger.LogWarning("[CircuitBreaker] '{Name}' is open, rejecting request. FailureRate: {FailureRate}%",
                        Name, metrics.FailurePercentage);
                    throw new CircuitBreakerOpenException(Name, metrics);
                }

                // Half-open state - sınırlı access
                if (_state == CircuitBreakerState.HalfOpen)
                {
                    if (!await _halfOpenSemaphore.WaitAsync(0, cancellationToken))
                    {
                        Interlocked.Increment(ref _rejectedRequests);

                        if (fallback != null && _settings.EnableFallback)
                        {
                            _logger.LogDebug("[CircuitBreaker] '{Name}' is half-open and busy, executing fallback", Name);
                            return await fallback();
                        }

                        throw new CircuitBreakerOpenException(Name, GetMetrics());
                    }
                }

                try
                {
                    // Operation execute
                    Interlocked.Increment(ref _totalRequests);
                    var result = await operation(cancellationToken);

                    // Success handling
                    await OnSuccessAsync(stopwatch.Elapsed);

                    _logger.LogDebug("[CircuitBreaker] '{Name}' operation succeeded in {Duration}ms",
                        Name, stopwatch.ElapsedMilliseconds);

                    return result;
                }
                catch (Exception ex)
                {
                    // Failure handling
                    await OnFailureAsync(ex, stopwatch.Elapsed);
                    throw;
                }
                finally
                {
                    if (_state == CircuitBreakerState.HalfOpen)
                    {
                        _halfOpenSemaphore.Release();
                    }
                }
            }
            finally
            {
                if (_settings.EnableMetrics)
                {
                    RecordResponseTime(stopwatch.Elapsed);
                }
            }
        }

        /// <summary>
        /// Circuit breaker'ı manuel reset eder
        /// </summary>
        public void Reset()
        {
            lock (_stateLock)
            {
                var oldState = _state;
                ChangeState(CircuitBreakerState.Closed, "Manual reset");
                _halfOpenSuccessCount = 0;

                // Metrics reset
                Interlocked.Exchange(ref _totalRequests, 0);
                Interlocked.Exchange(ref _successfulRequests, 0);
                Interlocked.Exchange(ref _failedRequests, 0);
                Interlocked.Exchange(ref _rejectedRequests, 0);
                _lastFailure = null;
                _lastSuccess = null;

                ClearRequestWindow();

                _logger.LogInformation("[CircuitBreaker] '{Name}' manually reset from {OldState} to {NewState}",
                    Name, oldState, _state);
            }
        }

        /// <summary>
        /// Circuit breaker'ı manuel trip eder (Open state)
        /// </summary>
        public void Trip(string? reason = null)
        {
            lock (_stateLock)
            {
                var oldState = _state;
                ChangeState(CircuitBreakerState.Open, reason ?? "Manual trip");

                _logger.LogWarning("[CircuitBreaker] '{Name}' manually tripped from {OldState} to {NewState}. Reason: {Reason}",
                    Name, oldState, _state, reason ?? "Manual trip");
            }
        }

        /// <summary>
        /// Health check yapar
        /// </summary>
        public bool IsHealthy()
        {
            if (_state == CircuitBreakerState.Open)
                return false;

            var metrics = GetMetrics();

            // Minimum throughput kontrolü
            if (metrics.TotalRequests < _settings.MinimumThroughput)
                return true; // Yeterli veri yok

            // Failure percentage kontrolü
            return metrics.FailurePercentage <= _settings.FailurePercentageThreshold;
        }

        /// <summary>
        /// Current metrics getirir
        /// </summary>
        public CircuitBreakerMetrics GetMetrics()
        {
            lock (_metricsLock)
            {
                return new CircuitBreakerMetrics
                {
                    Name = Name,
                    State = _state,
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    RejectedRequests = _rejectedRequests,
                    LastStateChange = _lastStateChange,
                    LastFailure = _lastFailure,
                    LastSuccess = _lastSuccess,
                    AverageResponseTime = _responseTimes.Count > 0
                        ? TimeSpan.FromMilliseconds(_responseTimes.Average(t => t.TotalMilliseconds))
                        : TimeSpan.Zero
                };
            }
        }

        /// <summary>
        /// Success durumunu handle eder
        /// </summary>
        private async Task OnSuccessAsync(TimeSpan duration)
        {
            Interlocked.Increment(ref _successfulRequests);
            _lastSuccess = DateTime.UtcNow;

            // Request window'a ekle
            AddRequestRecord(new RequestRecord
            {
                Timestamp = DateTime.UtcNow,
                IsSuccess = true,
                Duration = duration
            });

            // Half-open state'de success count arttır
            if (_state == CircuitBreakerState.HalfOpen)
            {
                lock (_stateLock)
                {
                    _halfOpenSuccessCount++;

                    if (_halfOpenSuccessCount >= _settings.SuccessThreshold)
                    {
                        ChangeState(CircuitBreakerState.Closed, $"Success threshold reached ({_halfOpenSuccessCount})");
                        _halfOpenSuccessCount = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Failure durumunu handle eder
        /// </summary>
        private async Task OnFailureAsync(Exception exception, TimeSpan duration)
        {
            Interlocked.Increment(ref _failedRequests);
            _lastFailure = DateTime.UtcNow;

            // Request window'a ekle
            AddRequestRecord(new RequestRecord
            {
                Timestamp = DateTime.UtcNow,
                IsSuccess = false,
                Duration = duration,
                ErrorType = exception.GetType().Name
            });

            _logger.LogWarning("[CircuitBreaker] '{Name}' operation failed: {Error}",
                Name, exception.Message);

            // State değerlendirmesi
            await CheckAndUpdateStateAsync();
        }

        /// <summary>
        /// State kontrolü ve güncellemesi
        /// </summary>
        private async Task CheckAndUpdateStateAsync()
        {
            // Closed -> Open geçişi
            if (_state == CircuitBreakerState.Closed)
            {
                if (ShouldTripToOpen())
                {
                    lock (_stateLock)
                    {
                        if (_state == CircuitBreakerState.Closed) // Double check
                        {
                            ChangeState(CircuitBreakerState.Open, "Failure threshold exceeded");
                        }
                    }
                }
            }
            // Open -> Half-Open geçişi
            else if (_state == CircuitBreakerState.Open)
            {
                if (DateTime.UtcNow - _lastStateChange >= _settings.Timeout)
                {
                    lock (_stateLock)
                    {
                        if (_state == CircuitBreakerState.Open &&
                            DateTime.UtcNow - _lastStateChange >= _settings.Timeout)
                        {
                            ChangeState(CircuitBreakerState.HalfOpen, "Timeout period elapsed");
                            _halfOpenSuccessCount = 0;
                        }
                    }
                }
            }
            // Half-Open'da failure varsa -> Open
            else if (_state == CircuitBreakerState.HalfOpen)
            {
                // OnFailureAsync'de zaten kontrol ediliyor
            }
        }

        /// <summary>
        /// Open state'e geçiş gerekip gerekmediğini kontrol eder
        /// </summary>
        private bool ShouldTripToOpen()
        {
            var windowStart = DateTime.UtcNow - _settings.SamplePeriod;
            var recentRequests = GetRecentRequests(windowStart);

            if (recentRequests.Count < _settings.MinimumThroughput)
                return false;

            var failureCount = recentRequests.Count(r => !r.IsSuccess);
            var failurePercentage = (double)failureCount / recentRequests.Count * 100;

            return failureCount >= _settings.FailureThreshold ||
                   failurePercentage >= _settings.FailurePercentageThreshold;
        }

        /// <summary>
        /// State değişimini handle eder
        /// </summary>
        private void ChangeState(CircuitBreakerState newState, string reason)
        {
            var oldState = _state;
            _state = newState;
            _lastStateChange = DateTime.UtcNow;

            var eventArgs = new CircuitBreakerEventArgs
            {
                Name = Name,
                OldState = oldState,
                NewState = newState,
                Reason = reason,
                Metrics = GetMetrics()
            };

            StateChanged?.Invoke(this, eventArgs);

            _logger.LogInformation("[CircuitBreaker] '{Name}' state changed: {OldState} -> {NewState}. Reason: {Reason}",
                Name, oldState, newState, reason);
        }

        /// <summary>
        /// Request window operations
        /// </summary>
        private void AddRequestRecord(RequestRecord record)
        {
            _requestWindow.Enqueue(record);
            CleanupOldRequests();
        }

        private void CleanupOldRequests()
        {
            var cutoff = DateTime.UtcNow - _settings.SamplePeriod;

            while (_requestWindow.TryPeek(out var oldestRecord) && oldestRecord.Timestamp < cutoff)
            {
                _requestWindow.TryDequeue(out _);
            }
        }

        private List<RequestRecord> GetRecentRequests(DateTime since)
        {
            return _requestWindow.Where(r => r.Timestamp >= since).ToList();
        }

        private void ClearRequestWindow()
        {
            while (_requestWindow.TryDequeue(out _)) { }
        }

        private void RecordResponseTime(TimeSpan duration)
        {
            lock (_metricsLock)
            {
                _responseTimes.Add(duration);

                // Son 100 response time'ı sakla
                if (_responseTimes.Count > 100)
                {
                    _responseTimes.RemoveAt(0);
                }
            }
        }

        public void Dispose()
        {
            _halfOpenSemaphore?.Dispose();
        }
    }

    /// <summary>
    /// Circuit Breaker factory
    /// </summary>
    public interface ICircuitBreakerFactory
    {
        IEnhancedCircuitBreaker GetCircuitBreaker(string name);
        IEnhancedCircuitBreaker CreateCircuitBreaker(string name, CircuitBreakerSettings? settings = null);
        IEnumerable<IEnhancedCircuitBreaker> GetAllCircuitBreakers();
        CircuitBreakerMetrics GetAggregatedMetrics();
    }

    /// <summary>
    /// Circuit Breaker factory implementasyonu
    /// </summary>
    public class CircuitBreakerFactory : ICircuitBreakerFactory
    {
        private readonly ConcurrentDictionary<string, IEnhancedCircuitBreaker> _circuitBreakers = new();
        private readonly ILogger<CircuitBreakerFactory> _logger;
        private readonly CircuitBreakerSettings _defaultSettings;

        public CircuitBreakerFactory(ILogger<CircuitBreakerFactory> logger, IOptions<CircuitBreakerSettings>? defaultSettings = null)
        {
            _logger = logger;
            _defaultSettings = defaultSettings?.Value ?? new CircuitBreakerSettings();
        }

        public IEnhancedCircuitBreaker GetCircuitBreaker(string name)
        {
            return _circuitBreakers.GetOrAdd(name, n => CreateCircuitBreaker(n));
        }

        public IEnhancedCircuitBreaker CreateCircuitBreaker(string name, CircuitBreakerSettings? settings = null)
        {
            var cbSettings = settings ?? new CircuitBreakerSettings
            {
                Name = name,
                FailureThreshold = _defaultSettings.FailureThreshold,
                Timeout = _defaultSettings.Timeout,
                SuccessThreshold = _defaultSettings.SuccessThreshold,
                SamplePeriod = _defaultSettings.SamplePeriod,
                FailurePercentageThreshold = _defaultSettings.FailurePercentageThreshold,
                MinimumThroughput = _defaultSettings.MinimumThroughput
            };

            var loggerFactory = (ILoggerFactory?)(_logger as ILoggerFactory);
            var logger = loggerFactory != null ? loggerFactory.CreateLogger<EnhancedCircuitBreaker>() : _logger as ILogger<EnhancedCircuitBreaker>;
            if (logger == null)
            {
                // Fallback: use generic logger with adapter if necessary
                throw new InvalidOperationException("Unable to create logger for EnhancedCircuitBreaker");
            }
            return new EnhancedCircuitBreaker(name, cbSettings, logger);
        }

        public IEnumerable<IEnhancedCircuitBreaker> GetAllCircuitBreakers()
        {
            return _circuitBreakers.Values.ToList();
        }

        public CircuitBreakerMetrics GetAggregatedMetrics()
        {
            var allMetrics = _circuitBreakers.Values.Select(cb => cb.GetMetrics()).ToList();

            return new CircuitBreakerMetrics
            {
                Name = "Aggregated",
                TotalRequests = allMetrics.Sum(m => m.TotalRequests),
                SuccessfulRequests = allMetrics.Sum(m => m.SuccessfulRequests),
                FailedRequests = allMetrics.Sum(m => m.FailedRequests),
                RejectedRequests = allMetrics.Sum(m => m.RejectedRequests),
                CustomMetrics = new Dictionary<string, object>
                {
                    { "OpenCircuitBreakers", allMetrics.Count(m => m.State == CircuitBreakerState.Open) },
                    { "HalfOpenCircuitBreakers", allMetrics.Count(m => m.State == CircuitBreakerState.HalfOpen) },
                    { "ClosedCircuitBreakers", allMetrics.Count(m => m.State == CircuitBreakerState.Closed) },
                    { "TotalCircuitBreakers", allMetrics.Count }
                }
            };
        }
    }
}
