using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformDashboard;

/// <summary>
/// Generic handler — single handler serves all 15 marketplace dashboards.
/// Queries Store→ProductPlatformMapping→Order chain for the given PlatformType.
/// G413: 14 marketplace VM handler.
/// </summary>
public sealed class GetPlatformDashboardHandler
    : IRequestHandler<GetPlatformDashboardQuery, PlatformDashboardDto>
{
    private readonly IStoreRepository _storeRepo;
    private readonly IProductPlatformMappingRepository _mappingRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ILogger<GetPlatformDashboardHandler> _logger;

    public GetPlatformDashboardHandler(
        IStoreRepository storeRepo,
        IProductPlatformMappingRepository mappingRepo,
        IOrderRepository orderRepo,
        ILogger<GetPlatformDashboardHandler> logger)
    {
        _storeRepo = storeRepo;
        _mappingRepo = mappingRepo;
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public async Task<PlatformDashboardDto> Handle(
        GetPlatformDashboardQuery request, CancellationToken cancellationToken)
    {
        var stores = await _storeRepo.GetByTenantIdAsync(request.TenantId, cancellationToken);
        var platformStore = stores.FirstOrDefault(s =>
            s.PlatformType == request.Platform && s.IsActive);

        if (platformStore is null)
        {
            return new PlatformDashboardDto
            {
                Platform = request.Platform,
                IsConnected = false,
                SyncStatus = "Baglanti yok"
            };
        }

        // Product count via platform mappings
        var productCount = await _mappingRepo.CountByStoreIdAsync(platformStore.Id, cancellationToken);

        // Recent orders (last 30 days)
        var since = DateTime.UtcNow.AddDays(-30);
        var orders = await _orderRepo.GetByDateRangeAsync(
            request.TenantId, since, DateTime.UtcNow, cancellationToken);
        var platformOrders = orders
            .Where(o => o.SourcePlatform == request.Platform)
            .ToList();

        var today = DateTime.UtcNow.Date;
        var todayOrders = platformOrders.Where(o => o.CreatedAt >= today).ToList();

        var recentOrderDtos = platformOrders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new PlatformRecentOrderDto
            {
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName ?? "-",
                Total = o.TotalAmount,
                Status = o.Status.ToString(),
                OrderDate = o.CreatedAt
            })
            .ToList();

        _logger.LogDebug(
            "PlatformDashboard {Platform}: {ProductCount} products, {OrderCount} orders (30d), {TodayRevenue} today revenue",
            request.Platform, productCount, platformOrders.Count, todayOrders.Sum(o => o.TotalAmount));

        return new PlatformDashboardDto
        {
            Platform = request.Platform,
            IsConnected = true,
            ProductCount = productCount,
            OrderCount = platformOrders.Count,
            DailyRevenue = todayOrders.Sum(o => o.TotalAmount),
            SyncStatus = platformStore.LastSettlementDate.HasValue ? "Aktif" : "Bekliyor",
            LastSyncAt = platformStore.LastSettlementDate,
            RecentOrders = recentOrderDtos
        };
    }
}
