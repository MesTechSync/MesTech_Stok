#pragma warning disable MA0051 // Method is too long — FIFO algorithm is a single cohesive unit
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Services.Accounting;

/// <summary>
/// FIFO (First In, First Out) yontemi ile satilan mal maliyeti (COGS) hesaplama servisi.
///
/// Algoritma:
/// 1. Urune ait tum StockMovement kayitlarini tarihe gore siralar.
/// 2. Giris hareketlerinden (Purchase, StockIn, CustomerReturn, PlatformReturn, Production, Found, BarcodeReceive)
///    FIFO kuyruguna katman ekler: (miktar, birim_maliyet, tarih).
/// 3. Cikis hareketlerinden (Sale, BarcodeSale, PlatformSale, StockOut, Consumption, Loss)
///    en eski katmandan baslayarak tuketir ve COGS toplar.
/// 4. Kalan katmanlar = envanter maliyeti.
/// </summary>
public sealed class FifoCostCalculationService : IFifoCostCalculationService
{
    private readonly IStockMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<FifoCostCalculationService> _logger;

    // Giris hareket turleri — FIFO katmani olusturur
    private static readonly HashSet<string> InboundTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(StockMovementType.StockIn),
        nameof(StockMovementType.Purchase),
        nameof(StockMovementType.BarcodeReceive),
        nameof(StockMovementType.Production),
        nameof(StockMovementType.CustomerReturn),
        nameof(StockMovementType.Found),
        nameof(StockMovementType.PlatformReturn),
#pragma warning disable CS0618 // Obsolete enum values used for backward compatibility
        nameof(StockMovementType.In)
#pragma warning restore CS0618
    };

    // Cikis hareket turleri — FIFO katmanindan tuketir
    private static readonly HashSet<string> OutboundTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(StockMovementType.StockOut),
        nameof(StockMovementType.Sale),
        nameof(StockMovementType.BarcodeSale),
        nameof(StockMovementType.Consumption),
        nameof(StockMovementType.Loss),
        nameof(StockMovementType.PlatformSale),
#pragma warning disable CS0618 // Obsolete enum values used for backward compatibility
        nameof(StockMovementType.Out)
#pragma warning restore CS0618
    };

    // COGS hesabini ETKILEMEYEN hareket turleri — duzeltme, transfer, sync
    // Bu hareketler gercek alis/satis degil, FIFO katmanlarini degistirmemeli.
    private static readonly HashSet<string> SkippedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(StockMovementType.Adjustment),
        nameof(StockMovementType.Transfer),
        nameof(StockMovementType.PlatformSync),
        nameof(StockMovementType.OpenCartSync),
        nameof(StockMovementType.TrendyolSync),
        nameof(StockMovementType.MarketplaceSync),
        nameof(StockMovementType.None)
    };

    public FifoCostCalculationService(
        IStockMovementRepository movementRepository,
        IProductRepository productRepository,
        ILogger<FifoCostCalculationService> logger)
    {
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FifoCostResultDto> CalculateCOGSAsync(
        Guid tenantId, Guid productId, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, ct);
        if (product == null || product.TenantId != tenantId)
        {
            _logger.LogWarning("Product {ProductId} not found for tenant {TenantId}", productId, tenantId);
            return CreateEmptyResult(productId);
        }

        var movements = await _movementRepository.GetByProductIdAsync(productId, ct);

        // Filter by tenant and sort chronologically
        var sorted = movements
            .Where(m => m.TenantId == tenantId && !m.IsReversed)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.CreatedAt)
            .ToList();

        return CalculateFifo(product, sorted);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FifoCostResultDto>> CalculateAllCOGSAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var products = await _productRepository.GetAllAsync(ct);
        var tenantProducts = products.Where(p => p.TenantId == tenantId && !p.IsDeleted).ToList();

        // Batch query — N+1 yerine tek SQL (100 ürün = 1 query instead of 100)
        var productIds = tenantProducts.Select(p => p.Id).ToList();
        var allMovements = await _movementRepository.GetByProductIdsAsync(productIds, ct);
        var movementsByProduct = allMovements
            .Where(m => m.TenantId == tenantId && !m.IsReversed)
            .GroupBy(m => m.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Date).ThenBy(m => m.CreatedAt).ToList());

        var results = new List<FifoCostResultDto>(tenantProducts.Count);

        foreach (var product in tenantProducts)
        {
            var sorted = movementsByProduct.TryGetValue(product.Id, out var list) ? list : new List<StockMovement>();
            results.Add(CalculateFifo(product, sorted));
        }

        return results.AsReadOnly();
    }

    /// <summary>
    /// FIFO hesaplama cekirdegi.
    /// Giris hareketlerinden katman kuyrugu olusturur, cikis hareketleriyle en eskiden tuketir.
    /// </summary>
    private FifoCostResultDto CalculateFifo(Product product, List<StockMovement> sortedMovements)
    {
        // FIFO queue: LinkedList for efficient front removal
        var layers = new LinkedList<MutableFifoLayer>();
        var totalCogs = 0m;
        var totalPurchased = 0;
        var totalSold = 0;

        foreach (var movement in sortedMovements)
        {
            var absQuantity = Math.Abs(movement.Quantity);

            if (IsInbound(movement))
            {
                // Determine unit cost: prefer movement's UnitCost, fallback to product PurchasePrice
                var unitCost = movement.UnitCost ?? product.PurchasePrice;

                layers.AddLast(new MutableFifoLayer
                {
                    PurchaseDate = movement.Date,
                    Quantity = absQuantity,
                    UnitCost = unitCost
                });

                totalPurchased += absQuantity;
            }
            else if (IsOutbound(movement))
            {
                var remaining = absQuantity;
                totalSold += absQuantity;

                while (remaining > 0 && layers.Count > 0)
                {
                    var firstNode = layers.First ?? throw new InvalidOperationException("FIFO layer unexpectedly empty");
                    var oldest = firstNode.Value;

                    if (oldest.Quantity <= remaining)
                    {
                        // Consume entire layer
                        totalCogs += oldest.Quantity * oldest.UnitCost;
                        remaining -= oldest.Quantity;
                        layers.RemoveFirst();
                    }
                    else
                    {
                        // Partial consumption from oldest layer
                        totalCogs += remaining * oldest.UnitCost;
                        oldest.Quantity -= remaining;
                        remaining = 0;
                    }
                }

                if (remaining > 0)
                {
                    _logger.LogWarning(
                        "FIFO underflow for product {SKU}: {Remaining} units sold without matching purchase layers",
                        product.SKU, remaining);
                }
            }
            // Adjustment and Transfer types are skipped — they don't affect COGS
        }

        // Build remaining layers DTO
        var remainingLayers = layers.Select(l => new FifoLayerDto
        {
            PurchaseDate = l.PurchaseDate,
            Quantity = l.Quantity,
            UnitCost = l.UnitCost
        }).ToList().AsReadOnly();

        var currentStock = remainingLayers.Sum(l => l.Quantity);
        var totalRemainingCost = remainingLayers.Sum(l => l.Quantity * l.UnitCost);
        var avgCostPerUnit = currentStock > 0
            ? Math.Round(totalRemainingCost / currentStock, 4)
            : 0m;

        return new FifoCostResultDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            TotalPurchased = totalPurchased,
            TotalSold = totalSold,
            CurrentStock = currentStock,
            TotalCOGS = Math.Round(totalCogs, 2),
            AverageCostPerUnit = avgCostPerUnit,
            RemainingLayers = remainingLayers
        };
    }

    private static bool IsInbound(StockMovement m) =>
        InboundTypes.Contains(m.MovementType) ||
        (m.Quantity > 0 && !OutboundTypes.Contains(m.MovementType) && !SkippedTypes.Contains(m.MovementType));

    private static bool IsOutbound(StockMovement m) =>
        OutboundTypes.Contains(m.MovementType) ||
        (m.Quantity < 0 && !InboundTypes.Contains(m.MovementType) && !SkippedTypes.Contains(m.MovementType));

    private static FifoCostResultDto CreateEmptyResult(Guid productId) => new()
    {
        ProductId = productId,
        RemainingLayers = Array.Empty<FifoLayerDto>()
    };

    /// <summary>
    /// Mutable FIFO layer — partial consumption icin.
    /// </summary>
    private sealed class MutableFifoLayer
    {
        public DateTime PurchaseDate { get; init; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; init; }
    }
}
