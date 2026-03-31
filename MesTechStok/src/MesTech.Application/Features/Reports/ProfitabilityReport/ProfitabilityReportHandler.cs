#pragma warning disable MA0051 // Method is too long — report handler is a single cohesive operation
using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Reports.ProfitabilityReport;

/// <summary>
/// Karlılık raporu handler'i.
/// Net Kar = Satis Fiyati - Alis Maliyeti - Komisyon - Kargo Gideri - KDV
/// Her siparisteki her urunun PurchasePrice'ini Product entity'den cekerçek gercek maliyet hesaplar.
/// </summary>
public sealed class ProfitabilityReportHandler
    : IRequestHandler<ProfitabilityReportQuery, ProfitabilityReportDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProfitabilityReportHandler> _logger;

    public ProfitabilityReportHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ILogger<ProfitabilityReportHandler> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ProfitabilityReportDto> Handle(
        ProfitabilityReportQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Karlilik raporu: {From:dd.MM.yyyy} — {To:dd.MM.yyyy}",
            request.FromDate, request.ToDate);

        var orders = await _orderRepository.GetByDateRangeWithItemsAsync(
            request.TenantId, request.FromDate, request.ToDate, cancellationToken);

        // Sadece tamamlanmis siparisleri filtrele
        var completedOrders = orders
            .Where(o => o.Status == Domain.Enums.OrderStatus.Delivered ||
                        o.DeliveredAt.HasValue)
            .ToList();

        if (completedOrders.Count == 0)
        {
            return new ProfitabilityReportDto
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate
            };
        }

        // Urun maliyetlerini batch query ile cek (tek SQL — N+1 onleme)
        var productIds = completedOrders
            .SelectMany(o => o.OrderItems)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken).ConfigureAwait(false);
        var productCosts = products.ToDictionary(p => p.Id, p => p.PurchasePrice);

        // Her siparis icin kar hesapla
        decimal totalRevenue = 0, totalCost = 0, totalCommission = 0, totalShipping = 0, totalTax = 0;
        var platformAccum = new Dictionary<string, PlatformAccum>(StringComparer.Ordinal);
        var productAccum = new Dictionary<Guid, ProductAccum>();

        foreach (var order in completedOrders)
        {
            var orderRevenue = order.TotalAmount;
            var orderCommission = order.CommissionAmount ?? 0;
            var orderShipping = order.CargoExpenseAmount ?? 0;
            var orderTax = order.OrderItems.Sum(i => i.TaxAmount);
            decimal orderCost = 0;

            foreach (var item in order.OrderItems)
            {
                var unitCost = productCosts.GetValueOrDefault(item.ProductId, 0);
                var itemCost = unitCost * item.Quantity;
                orderCost += itemCost;

                // Urun bazli birikimç
                if (!productAccum.ContainsKey(item.ProductId))
                    productAccum[item.ProductId] = new ProductAccum(item.ProductSKU, item.ProductName);
                productAccum[item.ProductId].Add(item.Quantity, item.TotalPrice, itemCost);
            }

            totalRevenue += orderRevenue;
            totalCost += orderCost;
            totalCommission += orderCommission;
            totalShipping += orderShipping;
            totalTax += orderTax;

            // Platform bazli
            var platform = order.SourcePlatform?.ToString() ?? "Direct";
            if (!platformAccum.ContainsKey(platform))
                platformAccum[platform] = new PlatformAccum();
            platformAccum[platform].Add(orderRevenue, orderCost, orderCommission, orderShipping, orderTax);
        }

        var netProfit = totalRevenue - totalCost - totalCommission - totalShipping - totalTax;
        var profitMargin = totalRevenue > 0 ? Math.Round(netProfit / totalRevenue * 100, 2) : 0;

        // En karli ve en zararli urunler
        var allProducts = productAccum.Select(kv =>
        {
            var np = kv.Value.Revenue - kv.Value.Cost;
            var pm = kv.Value.Revenue > 0 ? Math.Round(np / kv.Value.Revenue * 100, 2) : 0;
            return new ProductProfitDto(
                kv.Key, kv.Value.SKU, kv.Value.Name,
                kv.Value.Quantity, Math.Round(kv.Value.Revenue, 2),
                Math.Round(kv.Value.Cost, 2), Math.Round(np, 2), pm);
        }).ToList();

        return new ProfitabilityReportDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalRevenue = Math.Round(totalRevenue, 2),
            TotalCost = Math.Round(totalCost, 2),
            TotalCommission = Math.Round(totalCommission, 2),
            TotalShipping = Math.Round(totalShipping, 2),
            TotalTax = Math.Round(totalTax, 2),
            NetProfit = Math.Round(netProfit, 2),
            ProfitMargin = profitMargin,
            TotalOrders = completedOrders.Count,
            ByPlatform = platformAccum.Select(kv =>
            {
                var np = kv.Value.Revenue - kv.Value.Cost - kv.Value.Commission - kv.Value.Shipping - kv.Value.Tax;
                var pm = kv.Value.Revenue > 0 ? Math.Round(np / kv.Value.Revenue * 100, 2) : 0;
                return new PlatformProfitDto(
                    kv.Key, kv.Value.Count,
                    Math.Round(kv.Value.Revenue, 2), Math.Round(kv.Value.Cost, 2),
                    Math.Round(kv.Value.Commission, 2), Math.Round(kv.Value.Shipping, 2),
                    Math.Round(kv.Value.Tax, 2),
                    Math.Round(np, 2), pm);
            }).OrderByDescending(p => p.NetProfit).ToList(),
            TopProfitableProducts = allProducts
                .OrderByDescending(p => p.NetProfit).Take(20).ToList(),
            LeastProfitableProducts = allProducts
                .Where(p => p.NetProfit < 0)
                .OrderBy(p => p.NetProfit).Take(10).ToList()
        };
    }

    private sealed class PlatformAccum
    {
        public int Count { get; private set; }
        public decimal Revenue { get; private set; }
        public decimal Cost { get; private set; }
        public decimal Commission { get; private set; }
        public decimal Shipping { get; private set; }
        public decimal Tax { get; private set; }
        public void Add(decimal rev, decimal cost, decimal comm, decimal shipping, decimal tax)
        {
            Count++; Revenue += rev; Cost += cost; Commission += comm; Shipping += shipping; Tax += tax;
        }
    }

    private sealed class ProductAccum
    {
        public string SKU { get; }
        public string Name { get; }
        public int Quantity { get; private set; }
        public decimal Revenue { get; private set; }
        public decimal Cost { get; private set; }
        public ProductAccum(string sku, string name) { SKU = sku; Name = name; }
        public void Add(int qty, decimal rev, decimal cost)
        {
            Quantity += qty; Revenue += rev; Cost += cost;
        }
    }
}
