using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace MesTechStok.Core.Data;

/// <summary>
/// Design-time factory to scaffold migrations for selected provider.
/// Provider: PostgreSQL (tek provider — Dalga 1 stabilizasyon).
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

        var pgConn = Environment.GetEnvironmentVariable("MESTECH_PG_CONNECTION");
        var defaultConn = configuration.GetConnectionString("DefaultConnection");
        var connectionString = pgConn ?? defaultConn ?? string.Empty;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=mestech_stok;Username=mestech_user;Password=CONFIGURE_VIA_USER_SECRETS";
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.CommandTimeout(120);
            npgsql.EnableRetryOnFailure(5);
        });

        return new AppDbContext(optionsBuilder.Options);
    }
}
