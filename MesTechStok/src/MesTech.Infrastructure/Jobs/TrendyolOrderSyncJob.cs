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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TrendyolOrderSyncJob> _logger;

    public TrendyolOrderSyncJob(
        IAdapterFactory factory,
        IOrderRepository orderRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<TrendyolOrderSyncJob> logger)
    {
        _factory = factory;
        _orderRepo = orderRepo;
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
            int created = 0, skipped = 0;

            foreach (var dto in orders)
            {
                // Duplicate check — ayni PlatformOrderId zaten var mi?
                var existing = await _orderRepo.GetByOrderNumberAsync(dto.OrderNumber, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    skipped++;
                    continue;
                }

                var order = MapToOrder(dto, tenantId);
                await _orderRepo.AddAsync(order, ct).ConfigureAwait(false);
                created++;
            }

            if (created > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[{JobId}] Trendyol siparis sync tamamlandi: {Total} cekildi, {Created} olusturuldu, {Skipped} mevcut (skip)",
                JobId, orders.Count, created, skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{JobId}] Trendyol siparis sync HATA", JobId);
            throw;
        }
    }

    private static Order MapToOrder(ExternalOrderDto dto, Guid tenantId)
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
            ShippingAddress = dto.CustomerAddress,
            OrderDate = dto.OrderDate,
            Notes = $"Trendyol sync — {dto.Status} — Total: {dto.TotalAmount:F2} {dto.Currency}"
        };

        // Set financials via domain method (private set properties)
        order.SetFinancials(dto.TotalAmount, 0, dto.TotalAmount);

        // Status mapping (OrderStatus enum: Pending/Confirmed/Shipped/Delivered/Cancelled)
        order.Status = dto.Status?.ToUpperInvariant() switch
        {
            "CREATED" or "AWAITING" => OrderStatus.Pending,
            "PICKING" or "INVOICED" or "APPROVED" => OrderStatus.Confirmed,
            "SHIPPED" => OrderStatus.Shipped,
            "DELIVERED" => OrderStatus.Delivered,
            "CANCELLED" or "RETURNED" => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };

        return order;
    }
}
