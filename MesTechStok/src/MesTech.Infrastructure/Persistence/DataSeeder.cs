using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// Seed data — Default Tenant + Store oluşturur.
/// İlk çalıştırmada veya migration sonrası çağrılır.
/// </summary>
public sealed class DataSeeder
{
    public static readonly Guid DefaultTenantId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static readonly Guid DefaultStoreId =
        Guid.Parse("00000000-0000-0000-0000-000000000010");

    private readonly AppDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedDefaultTenantAsync(ct);
        await SeedDefaultStoreAsync(ct);
        await MigrateOrphanedRecordsAsync(ct);
    }

    private async Task SeedDefaultTenantAsync(CancellationToken ct)
    {
        var exists = await _context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(t => t.Id == DefaultTenantId, ct);

        if (exists) return;

        var tenant = new Tenant
        {
            Name = "Default",
            IsActive = true
        };

        // Set Id via reflection since it has protected setter
        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(Domain.Common.BaseEntity.Id))!
            .SetValue(tenant, DefaultTenantId);

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Default Tenant olusturuldu: {TenantId}", DefaultTenantId);
    }

    private async Task SeedDefaultStoreAsync(CancellationToken ct)
    {
        var exists = await _context.Stores
            .IgnoreQueryFilters()
            .AnyAsync(s => s.Id == DefaultStoreId, ct);

        if (exists) return;

        var store = new Store
        {
            StoreName = "Default Store",
            TenantId = DefaultTenantId,
            PlatformType = PlatformType.OpenCart,
            IsActive = true
        };

        typeof(Domain.Common.BaseEntity)
            .GetProperty(nameof(Domain.Common.BaseEntity.Id))!
            .SetValue(store, DefaultStoreId);

        _context.Stores.Add(store);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Default Store olusturuldu: {StoreId}", DefaultStoreId);
    }

    private async Task MigrateOrphanedRecordsAsync(CancellationToken ct)
    {
        // TenantId == Guid.Empty olan kayıtları Default Tenant'a taşı
        var orphanedProducts = await _context.Products
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == Guid.Empty)
            .ToListAsync(ct);

        if (orphanedProducts.Count > 0)
        {
            foreach (var p in orphanedProducts)
                p.TenantId = DefaultTenantId;

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "{Count} orphaned Product kaydı Default Tenant'a taşındı", orphanedProducts.Count);
        }
    }
}
