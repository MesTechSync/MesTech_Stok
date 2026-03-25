using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace MesTech.Infrastructure.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public RabbitMqHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var host = _configuration["RabbitMQ:Host"] ?? "localhost";
            var port = _configuration.GetValue("RabbitMQ:Port", 3672);
            var user = _configuration["RabbitMQ:Username"] ?? "guest";
            var pass = _configuration["RabbitMQ:Password"] ?? "guest";

            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = pass
            };

            using var connection = await factory.CreateConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ baglantisi basarili")
                : HealthCheckResult.Degraded("RabbitMQ baglanti acik degil");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ baglantisi basarisiz", ex);
        }
    }
}
