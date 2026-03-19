using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;

public class GetDropshipDashboardHandler
    : IRequestHandler<GetDropshipDashboardQuery, DropshipDashboardDto>
{
    private readonly IDropshipSupplierRepository _supplierRepository;
    private readonly ISupplierFeedRepository _feedRepository;
    private readonly IDropshipProductRepository _productRepository;
    private readonly IDropshipOrderRepository _orderRepository;

    public GetDropshipDashboardHandler(
        IDropshipSupplierRepository supplierRepository,
        ISupplierFeedRepository feedRepository,
        IDropshipProductRepository productRepository,
        IDropshipOrderRepository orderRepository)
    {
        _supplierRepository = supplierRepository;
        _feedRepository = feedRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
    }

    public async Task<DropshipDashboardDto> Handle(
        GetDropshipDashboardQuery request,
        CancellationToken cancellationToken)
    {
        // Active suppliers
        var suppliers = await _supplierRepository
            .GetByTenantAsync(request.TenantId, cancellationToken);
        var activeSuppliers = suppliers.Where(s => s.IsActive).ToList();

        // Active feeds
        var activeFeedCount = await _feedRepository
            .GetActiveCountAsync(request.TenantId, cancellationToken);

        // Total dropship products
        var products = await _productRepository
            .GetByTenantAsync(request.TenantId, ct: cancellationToken);

        // Orders
        var orders = await _orderRepository
            .GetByTenantAsync(request.TenantId, cancellationToken);

        var pendingOrders = orders
            .Count(o => o.Status == DropshipOrderStatus.Pending);

        // Monthly revenue/profit calculation (current month)
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var monthlyOrders = orders
            .Where(o => o.CreatedAt >= monthStart)
            .ToList();

        // Build top suppliers (by order count)
        var supplierPerformance = activeSuppliers
            .Select(s =>
            {
                var supplierOrders = orders
                    .Where(o => o.DropshipSupplierId == s.Id)
                    .ToList();

                return new SupplierPerformanceDto
                {
                    SupplierId = s.Id,
                    Name = s.Name,
                    OrderCount = supplierOrders.Count,
                    Revenue = 0m, // Requires product price join — simplified
                    AvgMargin = 0m
                };
            })
            .OrderByDescending(sp => sp.OrderCount)
            .Take(5)
            .ToList();

        return new DropshipDashboardDto
        {
            ActiveSuppliers = activeSuppliers.Count,
            ActiveFeeds = activeFeedCount,
            TotalDropshipProducts = products.Count,
            PendingOrders = pendingOrders,
            MonthlyRevenue = 0m, // Requires price data from products
            MonthlyProfit = 0m,
            AverageMargin = 0m,
            TopSuppliers = supplierPerformance
        };
    }
}
