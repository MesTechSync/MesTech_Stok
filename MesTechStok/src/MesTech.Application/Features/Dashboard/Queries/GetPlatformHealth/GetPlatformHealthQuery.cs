using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;

/// <summary>
/// Platform saglik durumu sorgusu — her platform icin son sync ve hata sayisi.
/// </summary>
public record GetPlatformHealthQuery(Guid TenantId)
    : IRequest<IReadOnlyList<PlatformHealthDto>>;

/// <summary>
/// Platform saglik DTO.
/// </summary>
public record PlatformHealthDto
{
    public string Platform { get; init; } = string.Empty;
    public DateTime? LastSyncAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public int ErrorCount24h { get; init; }
}
