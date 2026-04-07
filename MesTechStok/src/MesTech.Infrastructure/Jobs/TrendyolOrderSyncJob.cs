using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her 5 dakikada Trendyol'dan yeni siparisleri ceker ve DB'ye persist eder.
/// Akis: PullOrdersAsync → duplicate check → ExternalOrderDto→Order mapping → AddAsync → SaveChanges.
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class TrendyolOrderSyncJob : ISyncJob
{
    public string JobId => "trendyol-order-sync";
    public string CronExpression => "*/5 * * * *"; // Her 5 dk

    private readonly IAdapterFactory _factory;
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TrendyolOrderSyncJob> _logger;

    public TrendyolOrderSyncJob(
        IAdapterFactory factory,
        IOrderRepository orderRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<TrendyolOrderSyncJob> logger)
    {
        _factory = factory;
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[{JobId}] Trendyol siparis sync basliyor...", JobId);

        try
        {
            var adapter = _factory.ResolveCapability<IOrderCapableAdapter>("Trendyol");
            if (adapter == null)
            {
                _logger.LogWarning("[{JobId}] Trendyol IOrderCapableAdapter bulunamadi, atlaniyor", JobId);
                return;
            }

            var since = DateTime.UtcNow.AddHours(-1);
            var orders = await adapter.PullOrdersAsync(since, ct).ConfigureAwait(false);

            if (orders.Count == 0)
            {
                _logger.LogDebug("[{JobId}] Yeni siparis yok (son 1 saat)", JobId);
                return;
            }

            var tenantId = _tenantProvider.GetCurrentTenantId();
            int created = 0, updated = 0, skipped = 0;

            foreach (var dto in orders)
            {
                // Duplicate check — ayni PlatformOrderId zaten var mi?
                var existing = await _orderRepo.GetByOrderNumberAsync(dto.OrderNumber, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    // Domain lifecycle ile status guncelle — event'ler tetiklenir
                    var previousStatus = existing.Status;
                    AdvanceOrderLifecycle(existing, dto.Status, dto.CargoTrackingNumber, dto.CargoProviderName);

                    if (existing.Status != previousStatus)
                    {
                        existing.Notes = $"Trendyol sync — {dto.Status} — Kargo: {dto.CargoProviderName}";
                        await _orderRepo.UpdateAsync(existing, ct).ConfigureAwait(false);
                        updated++;
                    }
                    else
                    {
                        skipped++;
                    }
                    continue;
                }

                var order = await MapToOrderAsync(dto, tenantId, ct).ConfigureAwait(false);
                await _orderRepo.AddAsync(order, ct).ConfigureAwait(false);
                created++;
            }

            if (created > 0 || updated > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Trendyol siparis sync tamamlandi: {Total} cekildi, {Created} yeni, {Updated} guncellendi, {Skipped} degismemis",
                JobId, orders.Count, created, updated, skipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol siparis sync HATA", JobId);
            throw;
        }
    }

    /// <summary>
    /// Z1 zinciri: ExternalOrderDto → Order + OrderItem (ProductId eşleştirmeli).
    /// Barcode/SKU ile Product lookup — eşleşirse ProductId set edilir, stok düşürme tetiklenir.
    /// </summary>
    private async Task<Order> MapToOrderAsync(ExternalOrderDto dto, Guid tenantId, CancellationToken ct)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = dto.OrderNumber,
            ExternalOrderId = dto.PlatformOrderId,
            PlatformOrderNumber = dto.OrderNumber,
            SourcePlatform = PlatformType.Trendyol,
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            RecipientPhone = dto.CustomerPhone,
            ShippingAddress = string.IsNullOrEmpty(dto.CustomerAddress) ? dto.CustomerCity : dto.CustomerAddress,
            OrderDate = dto.OrderDate,
            Notes = $"Trendyol sync — {dto.Status} — Kargo: {dto.CargoProviderName} — Takip: {dto.CargoTrackingNumber}"
        };

        // OrderItems — barcode/SKU ile Product eşleştirme (Z1 zinciri)
        foreach (var line in dto.Lines)
        {
            // Product matching: barcode → SKU fallback (Z1 zinciri — stok düşürme için kritik)
            Guid productId = Guid.Empty;
            if (!string.IsNullOrEmpty(line.Barcode))
            {
                var product = await _productRepo.GetByBarcodeAsync(line.Barcode, ct).ConfigureAwait(false);
                if (product is not null) productId = product.Id;
            }
            if (productId == Guid.Empty && !string.IsNullOrEmpty(line.SKU))
            {
                var product = await _productRepo.GetBySKUAsync(line.SKU, ct).ConfigureAwait(false);
                if (product is not null) productId = product.Id;
            }

            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderId = order.Id,
                ProductId = productId,
                ProductName = line.ProductName,
                ProductSKU = line.SKU ?? line.Barcode ?? "",
                TaxRate = line.TaxRate
            };
            item.SetQuantityAndPrice(line.Quantity, line.UnitPrice);
            order.AddItem(item);
        }

        // Financials — brut tutar ve indirim varsa ayristir
        // AddItem zaten CalculateTotals yapar ama grossAmount/discount varsa override et
        var gross = dto.GrossAmount ?? dto.TotalAmount;
        var discount = dto.TotalDiscount ?? 0m;
        var subTotal = gross - discount;
        var taxAmount = order.TaxAmount; // AddItem→CalculateTotals'tan gelen deger
        order.SetFinancials(subTotal, taxAmount, dto.TotalAmount);

        // Domain lifecycle — status'a gore event'li gecis (Z1/Z2/Z3 zinciri tetikler)
        AdvanceOrderLifecycle(order, dto.Status, dto.CargoTrackingNumber, dto.CargoProviderName);

        return order;
    }

    /// <summary>
    /// Trendyol status'una gore Order domain lifecycle'ini ilerletir.
    /// Domain metotlari (Place, MarkAsShipped, MarkAsDelivered) event firlattigi icin
    /// downstream handler'lar (stok dusurme, fatura, GL) tetiklenir.
    /// </summary>
    private static void AdvanceOrderLifecycle(Order order, string? trendyolStatus, string? trackingNumber, string? cargoProvider)
    {
        var target = trendyolStatus?.ToUpperInvariant() switch
        {
            "CREATED" or "AWAITING" => OrderStatus.Pending,
            "PICKING" or "INVOICED" or "APPROVED" => OrderStatus.Confirmed,
            "SHIPPED" => OrderStatus.Shipped,
            "DELIVERED" => OrderStatus.Delivered,
            "CANCELLED" or "RETURNED" => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };

        if (target == OrderStatus.Cancelled)
        {
            if (order.Status is OrderStatus.Pending or OrderStatus.Confirmed)
                order.Cancel("Trendyol sync — cancelled/returned");
            return;
        }

        // Pending → Confirmed (Place) — OrderPlacedEvent → stok dusurme (Z1)
        if (target is OrderStatus.Confirmed or OrderStatus.Shipped or OrderStatus.Delivered
            && order.Status == OrderStatus.Pending)
            order.Place();

        // Confirmed → Shipped — OrderShippedEvent
        if (target is OrderStatus.Shipped or OrderStatus.Delivered
            && order.Status == OrderStatus.Confirmed)
            order.MarkAsShipped(trackingNumber ?? "TRENDYOL-SYNC", MapCargoProvider(cargoProvider));

        // Shipped → Delivered — OrderCompletedEvent → Invoice → GL (Z2/Z3)
        if (target == OrderStatus.Delivered && order.Status == OrderStatus.Shipped)
            order.MarkAsDelivered();
    }

    private static CargoProvider MapCargoProvider(string? name) => name?.ToUpperInvariant() switch
    {
        var n when n?.Contains("YURTICI") == true => CargoProvider.YurticiKargo,
        var n when n?.Contains("ARAS") == true => CargoProvider.ArasKargo,
        var n when n?.Contains("SURAT") == true => CargoProvider.SuratKargo,
        var n when n?.Contains("MNG") == true => CargoProvider.MngKargo,
        var n when n?.Contains("PTT") == true => CargoProvider.PttKargo,
        var n when n?.Contains("UPS") == true => CargoProvider.UPS,
        var n when n?.Contains("DHL") == true => CargoProvider.DHL,
        var n when n?.Contains("HEPSIJET") == true => CargoProvider.Hepsijet,
        _ => CargoProvider.Other
    };
}
