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
        .WithImage("postgres:17")
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
            .EnableDetailedErrors()
            .LogTo(msg => System.Console.WriteLine($"[EF] {msg}"), Microsoft.Extensions.Logging.LogLevel.Error)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new AppDbContext(options, new HeadlessTestTenantProvider());
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var db = CreateDbContext();

        // SQL script'i dosyaya yaz — debug için
        try
        {
            var sql = db.Database.GenerateCreateScript();
            var sqlPath = Path.Combine(AppContext.BaseDirectory, "screenshots", "create_schema.sql");
            Directory.CreateDirectory(Path.GetDirectoryName(sqlPath)!);
            File.WriteAllText(sqlPath, sql);
            System.Console.WriteLine($"[HEADLESS-PG] SQL script yazildi: {sql.Length} karakter");

            // [ karakterini ara
            var bracketPos = sql.IndexOf('[');
            if (bracketPos >= 0)
            {
                var lineStart = sql.LastIndexOf('\n', bracketPos) + 1;
                var lineEnd = sql.IndexOf('\n', bracketPos);
                if (lineEnd < 0) lineEnd = sql.Length;
                System.Console.WriteLine($"[HEADLESS-PG] ILK '[' pozisyon {bracketPos}, satir: {sql[lineStart..lineEnd].Trim()}");
            }
        }
        catch (Exception exSql)
        {
            System.Console.WriteLine($"[HEADLESS-PG] SQL script olusturulamadi: {exSql.Message}");
        }

        // Şema oluştur
        try
        {
            await db.Database.EnsureCreatedAsync();
            System.Console.WriteLine("[HEADLESS-PG] EnsureCreatedAsync BASARILI");
        }
        catch (Exception exCreate)
        {
            System.Console.WriteLine($"[HEADLESS-PG] EnsureCreatedAsync FAIL: {exCreate.GetType().Name}: {exCreate.Message}");
            return;
        }

        try
        {
            await TestSeedDataFactory.SeedAsync(db);
            System.Console.WriteLine("[HEADLESS-PG] SeedAsync BASARILI");
        }
        catch (Exception exSeed)
        {
            System.Console.WriteLine($"[HEADLESS-PG] SeedAsync FAIL: {exSeed.GetType().Name}: {exSeed.Message}");
            var inner = exSeed.InnerException;
            while (inner != null)
            {
                System.Console.WriteLine($"[HEADLESS-PG] SEED INNER: {inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
            }
        }
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
