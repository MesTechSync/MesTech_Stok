using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace MesTech.Infrastructure.HealthChecks;

/// <summary>
/// Tenant-agnostic PostgreSQL health check — AppDbContext yerine dogrudan NpgsqlConnection kullanir.
/// AppDbContext ITenantProvider gerektirir → anonim /health endpoint'ten erisilemez.
/// Bu check sadece PG baglantisini dogrular, tenant scope'a ihtiyac duymaz.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public PostgresHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL baglantisi basarili");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL baglantisi basarisiz", ex);
        }
    }
}
