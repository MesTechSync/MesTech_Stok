using Hangfire;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// Production-safe demo mode service.
/// Creates a temporary demo tenant with sample data (30-minute TTL).
/// Hangfire cleanup job removes expired demo tenants automatically.
/// Rate-limited: max 10 demo tenants per hour (IP-based via endpoint).
/// </summary>
public sealed class DemoModeService
{
    private readonly AppDbContext _context;
    private readonly DemoDataSeeder _seeder;
    private readonly ILogger<DemoModeService> _logger;

    /// <summary>Demo tenants expire after this duration.</summary>
    public static readonly TimeSpan DemoTtl = TimeSpan.FromMinutes(30);

    public DemoModeService(
        AppDbContext context,
        DemoDataSeeder seeder,
        ILogger<DemoModeService> logger)
    {
        _context = context;
        _seeder = seeder;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new demo tenant with sample data.
    /// Returns the tenant ID for the demo session.
    /// </summary>
    public async Task<DemoSession> CreateDemoSessionAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[DemoMode] Creating new demo session");

        // Seed demo data (idempotent — won't duplicate if already exists)
        await _seeder.SeedAsync(ct).ConfigureAwait(false);

        var expiresAt = DateTime.UtcNow.Add(DemoTtl);

        _logger.LogInformation(
            "[DemoMode] Demo session created — TenantId={TenantId}, ExpiresAt={ExpiresAt}",
            DemoDataSeeder.DemoTenantId, expiresAt);

        // Schedule cleanup (fire-and-forget via Hangfire)
        BackgroundJob.Schedule<DemoModeService>(
            svc => svc.CleanupExpiredDemoAsync(CancellationToken.None),
            DemoTtl.Add(TimeSpan.FromMinutes(5))); // 5min grace period

        return new DemoSession(
            TenantId: DemoDataSeeder.DemoTenantId,
            TenantName: "Demo Sirket",
            ExpiresAt: expiresAt,
            DemoUser: "demo@mestech.tr",
            ProductCount: 10,
            OrderCount: 5);
    }

    /// <summary>
    /// Hangfire job: cleans up demo tenant data older than TTL.
    /// Runs on schedule and as fire-and-forget after each demo session.
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CleanupExpiredDemoAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[DemoMode] Cleanup: checking for expired demo data");

        // IgnoreQueryFilters: KASITLI — admin cleanup job, sabit DemoTenantId ile sorgular.
        // Tenant bypass riski YOK: DemoDataSeeder.DemoTenantId hardcoded GUID, user input DEĞİL.
        var demoTenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == DemoDataSeeder.DemoTenantId, ct)
            .ConfigureAwait(false);

        if (demoTenant is null)
        {
            _logger.LogDebug("[DemoMode] No demo tenant found — nothing to clean");
            return;
        }

        // Check if demo data is older than TTL
        if (demoTenant.CreatedAt.Add(DemoTtl.Add(TimeSpan.FromMinutes(5))) > DateTime.UtcNow)
        {
            _logger.LogDebug("[DemoMode] Demo tenant still within TTL — skipping cleanup");
            return;
        }

        // Deactivate tenant (soft delete — data preserved for audit)
        demoTenant.IsActive = false;
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("[DemoMode] Demo tenant deactivated — TenantId={TenantId}", DemoDataSeeder.DemoTenantId);
    }

    /// <summary>
    /// Registers recurring cleanup job (every 6 hours).
    /// </summary>
    public static void RegisterCleanupJob()
    {
        RecurringJob.AddOrUpdate<DemoModeService>(
            "demo-mode-cleanup",
            svc => svc.CleanupExpiredDemoAsync(CancellationToken.None),
            "0 */6 * * *"); // every 6 hours
    }
}

/// <summary>Demo session response DTO.</summary>
public sealed record DemoSession(
    Guid TenantId,
    string TenantName,
    DateTime ExpiresAt,
    string DemoUser,
    int ProductCount,
    int OrderCount);
