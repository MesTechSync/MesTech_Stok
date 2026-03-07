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

    protected void SetCurrentTenant(Guid tenantId)
    {
        TenantProvider.SetTenant(tenantId);
    }

    /// <summary>
    /// Global query filter statik olarak context olusturulurken yakalanir.
    /// IgnoreQueryFilters ile bypass edip, guncel tenant ID ile filtreliyoruz.
    /// </summary>
    protected IQueryable<T> ApplyTenantFilter<T>(IQueryable<T> query) where T : class
    {
        var baseQuery = query.IgnoreQueryFilters();
        if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
        {
            var tenantId = TenantProvider.GetCurrentTenantId();
            baseQuery = baseQuery.Where(e => ((ITenantEntity)e).TenantId == tenantId);
        }
        return baseQuery;
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}
