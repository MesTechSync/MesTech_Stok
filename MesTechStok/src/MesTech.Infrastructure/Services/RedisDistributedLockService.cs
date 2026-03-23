using MesTech.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Redis-based distributed lock using SET NX EX pattern.
/// Lua script ensures only the lock owner can release (no accidental unlock).
/// Returns null when Redis is unavailable — callers must handle gracefully.
/// </summary>
public sealed class RedisDistributedLockService : IDistributedLockService, IDisposable
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Lua script: release lock only if the stored value matches the owner token.
    /// Prevents accidental release by another process after expiry + re-acquire.
    /// </summary>
    private const string ReleaseLuaScript = """
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(
        IConfiguration configuration,
        ILogger<RedisDistributedLockService> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:Configuration"]
            ?? "localhost:6379";

        try
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            _redis = ConnectionMultiplexer.Connect(options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis connection failed for distributed lock service. Locks will not be acquired.");
            _redis = null!;
        }
    }

    /// <inheritdoc />
    public async Task<IAsyncDisposable?> AcquireLockAsync(
        string resourceKey,
        TimeSpan expiry = default,
        TimeSpan waitTimeout = default,
        CancellationToken ct = default)
    {
        if (_redis is null || !_redis.IsConnected)
        {
            _logger.LogWarning("Redis not connected. Lock not acquired for resource '{Resource}'.", resourceKey);
            return null;
        }

        if (expiry == default) expiry = DefaultExpiry;
        if (waitTimeout == default) waitTimeout = DefaultWaitTimeout;

        var lockKey = $"lock:{resourceKey}";
        var lockValue = Guid.NewGuid().ToString("N");
        var db = _redis.GetDatabase();
        var deadline = DateTime.UtcNow + waitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // SET NX EX — atomic acquire with expiry
                var acquired = await db.StringSetAsync(
                    lockKey,
                    lockValue,
                    expiry,
                    When.NotExists,
                    CommandFlags.DemandMaster).ConfigureAwait(false);

                if (acquired)
                {
                    _logger.LogDebug("Distributed lock acquired: {Key} (owner: {Owner}, expiry: {Expiry}s)",
                        lockKey, lockValue, expiry.TotalSeconds);

                    return new LockHandle(db, lockKey, lockValue, _logger);
                }
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis connection lost while acquiring lock '{Key}'.", lockKey);
                return null;
            }

            await Task.Delay(RetryDelay, ct).ConfigureAwait(false);
        }

        _logger.LogWarning("Lock acquisition timed out for resource '{Key}' after {Timeout}s.",
            lockKey, waitTimeout.TotalSeconds);
        return null;
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }

    /// <summary>
    /// IAsyncDisposable handle that releases the lock via Lua script on dispose.
    /// </summary>
    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _value;
        private readonly ILogger _logger;
        private int _released;

        public LockHandle(IDatabase db, string key, string value, ILogger logger)
        {
            _db = db;
            _key = key;
            _value = value;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 1)
                return;

            try
            {
                var result = await _db.ScriptEvaluateAsync(
                    ReleaseLuaScript,
                    new RedisKey[] { _key },
                    new RedisValue[] { _value }).ConfigureAwait(false);

                if ((long)result == 1)
                {
                    _logger.LogDebug("Distributed lock released: {Key} (owner: {Owner})", _key, _value);
                }
                else
                {
                    _logger.LogWarning(
                        "Lock release skipped — key '{Key}' no longer owned (expired or stolen). Owner: {Owner}",
                        _key, _value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release distributed lock '{Key}'.", _key);
            }
        }
    }
}
