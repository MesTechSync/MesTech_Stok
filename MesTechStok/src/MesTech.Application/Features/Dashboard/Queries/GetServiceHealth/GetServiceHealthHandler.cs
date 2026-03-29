using MesTech.Application.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;

/// <summary>
/// Altyapi saglik isleyicisi.
/// IInfrastructureHealthService uzerinden PostgreSQL/Redis/RabbitMQ durumunu sorgular.
/// </summary>
public sealed class GetServiceHealthHandler
    : IRequestHandler<GetServiceHealthQuery, IReadOnlyList<ServiceHealthDto>>
{
    private readonly IInfrastructureHealthService _healthService;

    public GetServiceHealthHandler(IInfrastructureHealthService healthService)
        => _healthService = healthService;

    public async Task<IReadOnlyList<ServiceHealthDto>> Handle(
        GetServiceHealthQuery request, CancellationToken cancellationToken)
    {
        var results = await _healthService.CheckAllAsync(cancellationToken);

        return results
            .Select(r => new ServiceHealthDto
            {
                ServiceName = r.ServiceName,
                IsHealthy = r.IsHealthy,
                ResponseTime = r.ResponseTimeMs ?? "—"
            })
            .ToList()
            .AsReadOnly();
    }
}
