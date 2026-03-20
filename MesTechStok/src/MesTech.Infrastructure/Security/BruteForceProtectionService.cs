using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Account lockout + progressive delay + IP rate limiting.
///
/// Kurallar:
///   - 5 başarısız deneme (15dk pencere) → 15dk lockout
///   - Progressive delay: 0s, 1s, 2s, 4s, lock
///   - IP bazlı: 20 login/dk sliding window (429 Too Many Requests)
///   - Lockout süresi dolunca sayaç sıfırlanır
///   - Başarılı login → sayaç sıfırlanır
///
/// Depolama: In-memory (LoginAttemptTracker delegate) + DB audit (LoginAttempt entity).
///
/// Kullanım (AuthEndpoints'de):
///   var check = await _bruteForce.CheckAsync(username, ip);
///   if (check.IsLocked) return Unauthorized("Hesabınız kilitli...");
///   if (check.Delay > TimeSpan.Zero) await Task.Delay(check.Delay);
///   // ... login logic ...
///   if (success) await _bruteForce.RecordSuccessAsync(username, ip);
///   else await _bruteForce.RecordFailureAsync(username, ip);
/// </summary>
public class BruteForceProtectionService
{
    private readonly LoginAttemptTracker _tracker;
    private readonly IpRateLimiter _ipLimiter;
    private readonly ILogger<BruteForceProtectionService> _logger;

    // Progressive delay: attempt 1=0s, 2=1s, 3=2s, 4=4s, 5=lock
    private static readonly TimeSpan[] ProgressiveDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    public BruteForceProtectionService(
        LoginAttemptTracker tracker,
        ILogger<BruteForceProtectionService> logger)
    {
        _tracker = tracker;
        _ipLimiter = new IpRateLimiter();
        _logger = logger;
    }

    public Task<BruteForceCheck> CheckAsync(string username, string ip, CancellationToken ct = default)
    {
        // IP rate limit check
        if (!_ipLimiter.IsAllowed(ip))
        {
            _logger.LogWarning("IP rate limit exceeded for {Ip}", ip);
            return Task.FromResult(new BruteForceCheck(
                IsLocked: false,
                Delay: TimeSpan.Zero,
                AttemptsRemaining: 0,
                LockedUntil: null,
                IsIpRateLimited: true));
        }

        // Account lockout check
        var (isLocked, remainingTime) = _tracker.CheckLockout(username);
        if (isLocked)
        {
            var lockedUntil = DateTimeOffset.UtcNow + (remainingTime ?? TimeSpan.Zero);
            return Task.FromResult(new BruteForceCheck(
                IsLocked: true,
                Delay: TimeSpan.Zero,
                AttemptsRemaining: 0,
                LockedUntil: lockedUntil,
                IsIpRateLimited: false));
        }

        return Task.FromResult(new BruteForceCheck(
            IsLocked: false,
            Delay: TimeSpan.Zero,
            AttemptsRemaining: 5,
            LockedUntil: null,
            IsIpRateLimited: false));
    }

    public Task<BruteForceFailureResult> RecordFailureAsync(string username, string ip, CancellationToken ct = default)
    {
        _ipLimiter.Record(ip);
        var (isNowLocked, attemptsRemaining, lockoutDuration) = _tracker.RecordFailedAttempt(username);

        if (isNowLocked)
        {
            _logger.LogWarning(
                "Account locked: {Username} from {Ip} — lockout {Duration}s",
                username, ip, lockoutDuration?.TotalSeconds ?? 0);
        }

        // Calculate progressive delay based on failed attempts (5 - remaining)
        var failedCount = 5 - attemptsRemaining;
        var delay = failedCount > 0 && failedCount <= ProgressiveDelays.Length
            ? ProgressiveDelays[failedCount - 1]
            : TimeSpan.Zero;

        return Task.FromResult(new BruteForceFailureResult(
            IsNowLocked: isNowLocked,
            AttemptsRemaining: attemptsRemaining,
            Delay: delay,
            LockoutDuration: lockoutDuration));
    }

    public Task RecordSuccessAsync(string username, string ip, CancellationToken ct = default)
    {
        _tracker.RecordSuccess(username);
        return Task.CompletedTask;
    }
}

public record BruteForceCheck(
    bool IsLocked,
    TimeSpan Delay,
    int AttemptsRemaining,
    DateTimeOffset? LockedUntil,
    bool IsIpRateLimited);

public record BruteForceFailureResult(
    bool IsNowLocked,
    int AttemptsRemaining,
    TimeSpan Delay,
    TimeSpan? LockoutDuration);

/// <summary>
/// IP bazlı sliding window rate limiter.
/// 20 login/dakika per IP.
/// </summary>
internal sealed class IpRateLimiter
{
    private const int MaxRequestsPerMinute = 20;
    private readonly Dictionary<string, Queue<DateTime>> _requests = new();
    private readonly object _lock = new();

    public bool IsAllowed(string ip)
    {
        lock (_lock)
        {
            CleanExpired(ip);
            if (!_requests.TryGetValue(ip, out var queue))
                return true;
            return queue.Count < MaxRequestsPerMinute;
        }
    }

    public void Record(string ip)
    {
        lock (_lock)
        {
            if (!_requests.ContainsKey(ip))
                _requests[ip] = new Queue<DateTime>();
            _requests[ip].Enqueue(DateTime.UtcNow);
        }
    }

    private void CleanExpired(string ip)
    {
        if (!_requests.TryGetValue(ip, out var queue))
            return;
        var cutoff = DateTime.UtcNow.AddMinutes(-1);
        while (queue.Count > 0 && queue.Peek() < cutoff)
            queue.Dequeue();
    }
}
