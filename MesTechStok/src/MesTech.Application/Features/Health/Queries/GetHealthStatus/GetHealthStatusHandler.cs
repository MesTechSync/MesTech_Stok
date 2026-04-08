using System.Diagnostics;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Health.Queries.GetHealthStatus;

public sealed class GetHealthStatusHandler : IRequestHandler<GetHealthStatusQuery, HealthStatusDto>
{
    private readonly ICacheService _cache;

    public GetHealthStatusHandler(ICacheService cache) => _cache = cache;

    public async Task<HealthStatusDto> Handle(GetHealthStatusQuery request, CancellationToken cancellationToken)
    {
        var services = new List<ServiceHealthDto>();

        // Redis health
        services.Add(await CheckServiceAsync("Redis", async () =>
        {
            await _cache.SetAsync("health-check", "ok", TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            var val = await _cache.GetAsync<string>("health-check", cancellationToken).ConfigureAwait(false);
            return string.Equals(val, "ok", StringComparison.Ordinal) ? null : "Cache read/write mismatch";
        }));

        var healthy = services.Count(s => s.IsHealthy);

        return new HealthStatusDto
        {
            Services = services,
            CheckedAt = DateTime.UtcNow,
            HealthyCount = healthy,
            UnhealthyCount = services.Count - healthy
        };
    }

    private static async Task<ServiceHealthDto> CheckServiceAsync(string name, Func<Task<string?>> check)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var error = await check().ConfigureAwait(false);
            sw.Stop();
            return new ServiceHealthDto
            {
                Name = name,
                IsHealthy = error is null,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ErrorMessage = error
            };
        }
#pragma warning disable CA1031 // Intentional: health check must not throw — report degraded status
        catch (Exception ex)
        {
            sw.Stop();
            return new ServiceHealthDto
            {
                Name = name,
                IsHealthy = false,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
        }
#pragma warning restore CA1031
    }
}
