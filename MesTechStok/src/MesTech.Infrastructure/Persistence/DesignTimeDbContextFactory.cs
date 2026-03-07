using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MesTech.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations.
/// dotnet ef migrations add ... --project src/MesTech.Infrastructure
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MESTECH_PG_CONNECTION")
            ?? throw new InvalidOperationException(
                "MESTECH_PG_CONNECTION environment variable is not set. " +
                "Set it before running migrations: export MESTECH_PG_CONNECTION='Host=localhost;Port=5432;Database=mestech_stok;Username=...;Password=...'");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        });

        return new AppDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private class DesignTimeTenantProvider : ITenantProvider
    {
        public Guid GetCurrentTenantId() => Guid.Parse("00000000-0000-0000-0000-000000000001");
    }
}
