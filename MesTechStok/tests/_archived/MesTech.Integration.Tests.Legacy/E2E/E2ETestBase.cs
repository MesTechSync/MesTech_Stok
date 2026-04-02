using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace MesTech.Integration.Tests.E2E;

/// <summary>
/// E2E testlerin base class'i.
/// Testcontainers ile gercek PostgreSQL 17 container baslatir.
/// Docker yoksa test skip olur.
/// </summary>
[Trait("Requires", "Docker")]
public abstract class E2ETestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _pgContainer;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DockerHelper.SkipIfNoDocker();

        _pgContainer = new PostgreSqlBuilder()
            .WithDatabase("mestech_e2e")
            .WithUsername("mestech_test")
            .WithPassword("test_password_e2e")
            .WithImage("postgres:17-alpine")
            .Build();

        await _pgContainer.StartAsync();

        var services = new ServiceCollection();

        // Logging
        services.AddLogging();

        // DbContext — gercek PostgreSQL (Testcontainers)
        // FUTURE: AppDbContext mevcut olunca aktiflestirilecek:
        // services.AddDbContext<AppDbContext>(opts =>
        //     opts.UseNpgsql(_pgContainer.GetConnectionString()));

        // MediatR — Application CQRS handlers
        // FUTURE: Assembly marker mevcut olunca aktiflestirilecek:
        // services.AddMediatR(cfg =>
        //     cfg.RegisterServicesFromAssembly(typeof(MesTech.Application.AssemblyMarker).Assembly));

        ServiceProvider = services.BuildServiceProvider();

        // FUTURE: Migration uygula (AppDbContext hazir olunca)
        // var db = ServiceProvider.GetRequiredService<AppDbContext>();
        // await db.Database.MigrateAsync();

        await SeedAsync();
    }

    public async Task DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
            disposable.Dispose();
        if (_pgContainer != null)
            await _pgContainer.DisposeAsync();
    }

    /// <summary>Test verisi seed et — override edilebilir.</summary>
    protected virtual Task SeedAsync() => Task.CompletedTask;
}
