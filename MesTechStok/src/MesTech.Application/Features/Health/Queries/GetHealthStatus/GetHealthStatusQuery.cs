using MediatR;

namespace MesTech.Application.Features.Health.Queries.GetHealthStatus;

/// <summary>
/// All infrastructure services health check — Ekran 18 API Sağlık.
/// </summary>
public record GetHealthStatusQuery() : IRequest<HealthStatusDto>;

public sealed class HealthStatusDto
{
    public List<ServiceHealthDto> Services { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public int HealthyCount { get; set; }
    public int UnhealthyCount { get; set; }
}

public sealed class ServiceHealthDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Details { get; set; }
}
