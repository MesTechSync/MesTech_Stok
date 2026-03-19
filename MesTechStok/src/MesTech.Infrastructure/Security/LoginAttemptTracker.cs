namespace MesTech.Infrastructure.Security;

/// <summary>
/// Login denemelerini takip eder, brute-force koruması sağlar.
/// OWASP 2026: 5 deneme → progressive lockout (60→120→240→480→1800sn max).
/// Thread-safe (lock).
/// </summary>
public class LoginAttemptTracker
{
    private readonly Dictionary<string, LoginAttemptInfo> _attempts = new();
    private readonly object _lock = new();

    private const int MaxAttempts = 5;
    private const int InitialLockoutSeconds = 60;
    private const double LockoutMultiplier = 2.0;
    private const int MaxLockoutMinutes = 30;

    /// <summary>Kullanıcının kilitli olup olmadığını kontrol eder.</summary>
    public (bool IsLocked, TimeSpan? RemainingTime) CheckLockout(string username)
    {
        lock (_lock)
        {
            if (!_attempts.TryGetValue(username.ToLowerInvariant(), out var info))
                return (false, null);

            if (info.LockoutUntil.HasValue && info.LockoutUntil > DateTime.UtcNow)
            {
                var remaining = info.LockoutUntil.Value - DateTime.UtcNow;
                return (true, remaining);
            }

            return (false, null);
        }
    }

    /// <summary>Başarısız login denemesi kaydeder.</summary>
    public (bool IsNowLocked, int AttemptsRemaining, TimeSpan? LockoutDuration) RecordFailedAttempt(string username)
    {
        lock (_lock)
        {
            var key = username.ToLowerInvariant();
            if (!_attempts.ContainsKey(key))
                _attempts[key] = new LoginAttemptInfo();

            var info = _attempts[key];
            info.FailedCount++;
            info.LastAttempt = DateTime.UtcNow;

            if (info.FailedCount >= MaxAttempts)
            {
                var lockoutSeconds = InitialLockoutSeconds * Math.Pow(LockoutMultiplier, info.LockoutCount);
                lockoutSeconds = Math.Min(lockoutSeconds, MaxLockoutMinutes * 60);
                info.LockoutUntil = DateTime.UtcNow.AddSeconds(lockoutSeconds);
                info.LockoutCount++;
                info.FailedCount = 0;

                return (true, 0, TimeSpan.FromSeconds(lockoutSeconds));
            }

            return (false, MaxAttempts - info.FailedCount, null);
        }
    }

    /// <summary>Başarılı login — sayacı sıfırla.</summary>
    public void RecordSuccess(string username)
    {
        lock (_lock)
        {
            _attempts.Remove(username.ToLowerInvariant());
        }
    }

    private class LoginAttemptInfo
    {
        public int FailedCount { get; set; }
        public int LockoutCount { get; set; }
        public DateTime? LockoutUntil { get; set; }
        public DateTime LastAttempt { get; set; }
    }
}
