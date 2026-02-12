using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace MesTechStok.Core.Data;

/// <summary>
/// Design-time factory to scaffold migrations for selected provider.
/// Provider önceliği: Env var > appsettings.json. Desteklenen: SqlServer, PostgreSQL.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile(Path.Combine("..", "MesTechStok.Desktop", "appsettings.json"), optional: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        // Sağlayıcı sabit: SQL Server
        var sqlServerConn = Environment.GetEnvironmentVariable("MESTECH_SQLSERVER_CONNECTION");
        var defaultConn = configuration.GetConnectionString("DefaultConnection");
        var connectionString = sqlServerConn ?? defaultConn ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Server=localhost\\SQLEXPRESS;Database=MesTech_stok;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.CommandTimeout(120);
            sql.EnableRetryOnFailure(5);
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
