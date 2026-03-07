using Testcontainers.PostgreSql;

namespace MesTech.Tests.Integration.Fixtures;

/// <summary>
/// xUnit class fixture: spins up a real PostgreSQL 17 container via Testcontainers.
/// Shared across all tests in a collection — container starts once, reused by all tests.
/// Requires Docker Desktop running locally.
/// CI/CD integration (Docker-in-Docker) will be handled in Dalga 2 by DEV 4.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("mestech_test")
        .WithUsername("test_user")
        .WithPassword("test_pass")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync().AsTask();
    }
}
