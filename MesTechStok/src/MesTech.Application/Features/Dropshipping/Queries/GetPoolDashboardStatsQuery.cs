using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetPoolDashboardStatsQuery : IRequest<PoolDashboardStatsDto>;

public record PoolDashboardStatsDto(
    int TotalPoolProducts,
    int GreenCount,
    int YellowCount,
    int OrangeCount,
    int RedCount,
    int ActiveFeedCount,
    decimal AverageReliabilityScore,
    string AverageReliabilityColor,
    DateTime? LastSyncAt
);

public class GetPoolDashboardStatsQueryHandler(
    IDropshippingPoolRepository poolRepo,
    ISupplierFeedRepository feedRepo,
    ITenantProvider tenantProvider
) : IRequestHandler<GetPoolDashboardStatsQuery, PoolDashboardStatsDto>
{
    public async Task<PoolDashboardStatsDto> Handle(
        GetPoolDashboardStatsQuery req, CancellationToken cancellationToken)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var stats = await poolRepo.GetStatsAsync(tenantId, cancellationToken);
        var feedCount = await feedRepo.GetActiveCountAsync(tenantId, cancellationToken);
        var lastSync = await feedRepo.GetLastSyncAtAsync(tenantId, cancellationToken);

        var avgColor = stats.AverageScore >= 90 ? "Green"
            : stats.AverageScore >= 70 ? "Yellow"
            : stats.AverageScore >= 50 ? "Orange" : "Red";

        return new PoolDashboardStatsDto(
            stats.Total,
            stats.Green,
            stats.Yellow,
            stats.Orange,
            stats.Red,
            feedCount,
            stats.AverageScore,
            avgColor,
            lastSync
        );
    }
}
