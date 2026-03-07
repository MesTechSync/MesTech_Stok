using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration._Shared;

/// <summary>
/// Integration testleri icin InMemory DB base class.
/// Her test kendi izole veritabanini kullanir.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly TestTenantProvider TenantProvider;

    protected IntegrationTestBase()
    {
        TenantProvider = new TestTenantProvider();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options, TenantProvider);
        Context.Database.EnsureCreated();
    }

    protected void SetCurrentTenant(int tenantId)
    {
        TenantProvider.SetTenant(tenantId);
    }

    /// <summary>
    /// InMemory provider global query filter desteklemez.
    /// Bu yuzden tenant filtresi manuel uygulanir.
    /// </summary>
    protected IQueryable<T> ApplyTenantFilter<T>(IQueryable<T> query) where T : class
    {
        if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
        {
            var tenantId = TenantProvider.GetCurrentTenantId();
            return query.Where(e => ((ITenantEntity)e).TenantId == tenantId);
        }
        return query;
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
