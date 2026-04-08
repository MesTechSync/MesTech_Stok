using System.Collections.Concurrent;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// In-process distributed lock fallback for dev/test environments
/// where Redis is not available. Uses ConcurrentDictionary + SemaphoreSlim.
/// NOT suitable for multi-process/multi-container production scenarios.
/// </summary>
public sealed class InProcessDistributedLockService : IDistributedLockService
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ILogger<InProcessDistributedLockService> _logger;

    public InProcessDistributedLockService(ILogger<InProcessDistributedLockService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IAsyncDisposable?> AcquireLockAsync(
        string resourceKey,
        TimeSpan expiry = default,
        TimeSpan waitTimeout = default,
        CancellationToken ct = default)
    {
        if (expiry == default) expiry = DefaultExpiry;
        if (waitTimeout == default) waitTimeout = DefaultWaitTimeout;

        var lockKey = $"lock:{resourceKey}";
        var semaphore = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        bool acquired;
        try
        {
            acquired = await semaphore.WaitAsync(waitTimeout, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Lock acquisition cancelled for resource '{Key}'.", lockKey);
            return null;
        }

        if (!acquired)
        {
            _logger.LogWarning(
                "InProcess lock acquisition timed out for resource '{Key}' after {Timeout}s.",
                lockKey, waitTimeout.TotalSeconds);
            return null;
        }

        _logger.LogDebug(
            "InProcess lock acquired: {Key} (expiry: {Expiry}s)",
            lockKey, expiry.TotalSeconds);

        return new InProcessLockHandle(semaphore, lockKey, expiry, _logger);
    }

    /// <summary>
    /// IAsyncDisposable handle that releases the semaphore on dispose.
    /// Auto-releases after expiry via a background timer.
    /// </summary>
    private sealed class InProcessLockHandle : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly string _key;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _expiryCts;
        private int _released;

        public InProcessLockHandle(
            SemaphoreSlim semaphore,
            string key,
            TimeSpan expiry,
            ILogger logger)
        {
            _semaphore = semaphore;
            _key = key;
            _logger = logger;
            _expiryCts = new CancellationTokenSource();

            // Auto-release after expiry (simulates Redis key TTL)
            _ = Task.Delay(expiry, _expiryCts.Token).ContinueWith(_ =>
            {
                if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
                {
                    _semaphore.Release();
                    _logger.LogWarning(
                        "InProcess lock expired and auto-released: {Key}", _key);
                }
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        public ValueTask DisposeAsync()
        {
            // Always cancel+dispose CTS to prevent resource leak
            // (if expiry timer won the race, CTS was never disposed)
            try { _expiryCts.Cancel(); } catch (ObjectDisposedException) { }
            try { _expiryCts.Dispose(); } catch (ObjectDisposedException) { }

            if (Interlocked.Exchange(ref _released, 1) == 1)
                return ValueTask.CompletedTask; // Timer already released semaphore

            try
            {
                _semaphore.Release();
                _logger.LogDebug("InProcess lock released: {Key}", _key);
            }
            catch (SemaphoreFullException)
            {
                // Already released by expiry timer (race condition edge case)
                _logger.LogDebug(
                    "InProcess lock release skipped — already released: {Key}", _key);
            }

            return ValueTask.CompletedTask;
        }
    }
}
