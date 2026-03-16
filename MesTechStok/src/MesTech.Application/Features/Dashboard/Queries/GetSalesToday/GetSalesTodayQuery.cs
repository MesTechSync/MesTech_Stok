using MediatR;

namespace MesTech.Application.Features.Dashboard.Queries.GetSalesToday;

/// <summary>
/// Bugunku satis ozeti sorgusu — bugun vs dun karsilastirmali.
/// </summary>
public record GetSalesTodayQuery(Guid TenantId)
    : IRequest<SalesTodayDto>;

/// <summary>
/// Bugunku satis ozet DTO.
/// </summary>
public record SalesTodayDto
{
    public decimal Today { get; init; }
    public decimal Yesterday { get; init; }
    public decimal ChangePercent { get; init; }
}
