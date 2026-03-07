using FluentAssertions;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;
using MesTech.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// Testcontainers integration test: verifies EF Core migrations run correctly
/// against a real PostgreSQL 17 container.
/// Requires Docker Desktop to be running.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Requires", "Docker")]
public class TestcontainersMigrationTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public TestcontainersMigrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            })
            .Options;

        return new AppDbContext(options, new TestTenantProvider());
    }

    [Fact]
    public async Task Migration_ShouldCreateAllTables_InRealPostgreSql()
    {
        // ARRANGE
        await using var context = CreateDbContext();

        // ACT: Apply all migrations
        await context.Database.MigrateAsync();

        // ASSERT: Verify key tables exist by querying information_schema
        var tables = await context.Database
            .SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")
            .ToListAsync();

        tables.Should().Contain("Products");
        tables.Should().Contain("Orders");
        tables.Should().Contain("StockMovements");
        tables.Should().Contain("Tenants");
        tables.Should().Contain("Stores");
        tables.Should().Contain("Invoices");
        tables.Should().Contain("__EFMigrationsHistory");
    }

    [Fact]
    public async Task Migration_ShouldSupportCrudOperations_InRealPostgreSql()
    {
        // ARRANGE
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();

        // ACT: Insert a tenant
        var newTenant = new MesTech.Domain.Entities.Tenant
        {
            Name = "Test Tenant",
            TaxNumber = "1234567890",
            IsActive = true,
        };
        context.Set<MesTech.Domain.Entities.Tenant>().Add(newTenant);
        await context.SaveChangesAsync();

        // ASSERT: Read back
        var tenant = await context.Set<MesTech.Domain.Entities.Tenant>()
            .FirstOrDefaultAsync(t => t.Id == newTenant.Id);

        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("Test Tenant");
    }

    private sealed class TestTenantProvider : ITenantProvider
    {
        public Guid GetCurrentTenantId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
