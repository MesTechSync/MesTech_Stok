using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Testcontainers PostgreSQL 17 — headless test altyapısı.
/// Container BİR KEZ başlar, tüm 172+ view aynı DB'yi paylaşır.
/// ICollectionFixture ile xUnit collection sharing.
/// </summary>
public class TestPostgresFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("mestech_headless_test")
        .WithUsername("headless_user")
        .WithPassword("headless_pass")
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new AppDbContext(options, new HeadlessTestTenantProvider());
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
        await TestSeedDataFactory.SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync().AsTask();
    }
}

/// <summary>
/// Headless test için sabit TenantId provider.
/// TestSeedDataFactory.TestTenantId ile uyumlu.
/// </summary>
public sealed class HeadlessTestTenantProvider : ITenantProvider
{
    public Guid GetCurrentTenantId() => TestSeedDataFactory.TestTenantId;
}

/// <summary>
/// xUnit collection definition — tüm headless testler aynı PostgreSQL container'ı paylaşır.
/// </summary>
[CollectionDefinition("HeadlessPostgresCollection")]
public class HeadlessPostgresCollection : ICollectionFixture<TestPostgresFactory>
{
}
