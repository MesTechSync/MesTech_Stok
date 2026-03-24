using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Enums;

namespace MesTech.Application.Features.Dropshipping.Queries.GetSupplierPerformance;

/// <summary>
/// Tedarikçi performans hesaplama handler'ı.
/// Sipariş verilerinden fulfillment oranı, ortalama teslim süresi ve rating hesaplar.
/// </summary>
public class GetSupplierPerformanceHandler
    : IRequestHandler<GetSupplierPerformanceQuery, List<SupplierPerformanceDto>>
{
    private readonly IDropshipSupplierRepository _supplierRepository;
    private readonly IDropshipOrderRepository _orderRepository;

    public GetSupplierPerformanceHandler(
        IDropshipSupplierRepository supplierRepository,
        IDropshipOrderRepository orderRepository)
    {
        _supplierRepository = supplierRepository;
        _orderRepository = orderRepository;
    }

    public async Task<List<SupplierPerformanceDto>> Handle(
        GetSupplierPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var suppliers = await _supplierRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var allOrders = await _orderRepository.GetByTenantAsync(request.TenantId, cancellationToken);

        var from = request.From ?? DateTime.MinValue;
        var to = request.To ?? DateTime.MaxValue;

        var result = new List<SupplierPerformanceDto>();

        foreach (var supplier in suppliers)
        {
            var orders = allOrders
                .Where(o => o.DropshipSupplierId == supplier.Id
                    && o.CreatedAt >= from
                    && o.CreatedAt <= to)
                .ToList();

            int totalOrders = orders.Count;
            int fulfilled = orders.Count(o => o.Status == DropshipOrderStatus.Delivered);
            int failed = orders.Count(o => o.Status == DropshipOrderStatus.Failed);

            // Ortalama teslim süresi (OrderedAt → DeliveredAt)
            var deliveredOrders = orders
                .Where(o => o.DeliveredAt.HasValue && o.OrderedAt.HasValue)
                .ToList();

            // Null-forgiving replaced: filtered by HasValue on line 53, using ?? fallback for analyzer compliance
            double avgDays = deliveredOrders.Count > 0
                ? deliveredOrders.Average(o =>
                    ((o.DeliveredAt ?? DateTime.MinValue)
                     - (o.OrderedAt ?? DateTime.MinValue)).TotalDays)
                : 0;

            // Return rate: failed / total (basitleştirilmiş)
            double returnRate = totalOrders > 0
                ? (double)failed / totalOrders * 100
                : 0;

            // Rating hesaplama (1-5 arası)
            double rating = CalculateRating(totalOrders, fulfilled, failed, avgDays);

            result.Add(new SupplierPerformanceDto
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                TotalOrders = totalOrders,
                FulfilledOrders = fulfilled,
                FailedOrders = failed,
                AverageFulfillmentDays = Math.Round(avgDays, 1),
                ReturnRate = Math.Round(returnRate, 2),
                Rating = Math.Round(rating, 1)
            });
        }

        return result.OrderByDescending(s => s.Rating).ToList();
    }

    /// <summary>
    /// Rating hesaplama: fulfillment oranı (%60 ağırlık) + hız (%20) + sipariş hacmi (%20).
    /// </summary>
    private static double CalculateRating(int total, int fulfilled, int failed, double avgDays)
    {
        if (total == 0)
            return 0;

        // Fulfillment oranı (0-5)
        double fulfillmentScore = (double)fulfilled / total * 5.0;

        // Hız skoru (hızlı=5, yavaş=1; 3 gün altı mükemmel, 14+ gün kötü)
        double speedScore = avgDays switch
        {
            <= 0 => 3.0,
            <= 3 => 5.0,
            <= 5 => 4.0,
            <= 7 => 3.0,
            <= 14 => 2.0,
            _ => 1.0
        };

        // Hacim bonusu (10+ sipariş tam puan, az sipariş cezalı)
        double volumeScore = total switch
        {
            >= 50 => 5.0,
            >= 20 => 4.0,
            >= 10 => 3.0,
            >= 5 => 2.5,
            _ => 2.0
        };

        // Ağırlıklı ortalama
        double rating = fulfillmentScore * 0.6 + speedScore * 0.2 + volumeScore * 0.2;

        return Math.Clamp(rating, 1.0, 5.0);
    }
}
