namespace MesTech.Application.Interfaces;

/// <summary>
/// Demo modu servisi — geçici demo tenant oluşturur, 30dk TTL.
/// WelcomeWindow "Demo Dene" butonu ve WebApi /api/v1/demo/start endpoint'i kullanır.
/// </summary>
public interface IDemoModeService
{
    /// <summary>
    /// Demo tenant + demo user + seed data oluşturur.
    /// </summary>
    Task<DemoSessionResult> CreateDemoSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Süresi dolmuş demo verilerini temizler (Hangfire job).
    /// </summary>
    Task CleanupExpiredDemoAsync(CancellationToken ct = default);
}

/// <summary>Demo oturum sonucu.</summary>
public sealed record DemoSessionResult(
    Guid TenantId,
    string TenantName,
    DateTime ExpiresAt,
    string DemoUsername,
    string DemoPassword,
    string DemoEmail,
    int ProductCount,
    int OrderCount);
