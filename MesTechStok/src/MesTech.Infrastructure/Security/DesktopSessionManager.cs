namespace MesTech.Infrastructure.Security;

/// <summary>
/// Desktop session state koruma.
/// Idle → dim overlay, Lock → WelcomeWindow (modal, session korunur).
/// Son açık view hatırlama.
/// </summary>
public class DesktopSessionManager
{
    public string? CurrentUsername { get; private set; }
    public Guid? CurrentTenantId { get; private set; }
    public DateTime LoginTime { get; private set; }
    public DateTime LastActivity { get; private set; }
    public string? LastNavigatedView { get; set; }

    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromMinutes(15);

    public void SetSession(string username, Guid tenantId)
    {
        CurrentUsername = username;
        CurrentTenantId = tenantId;
        LoginTime = DateTime.UtcNow;
        RecordActivity();
    }

    public void RecordActivity() => LastActivity = DateTime.UtcNow;

    public bool IsIdle => (DateTime.UtcNow - LastActivity) > IdleTimeout;
    public bool ShouldLock => (DateTime.UtcNow - LastActivity) > LockTimeout;
    public bool IsAuthenticated => CurrentUsername != null;

    public void Clear()
    {
        CurrentUsername = null;
        CurrentTenantId = null;
        LastNavigatedView = null;
    }
}
