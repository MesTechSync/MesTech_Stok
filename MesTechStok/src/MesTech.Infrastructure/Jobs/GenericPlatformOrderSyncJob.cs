using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Tüm platformlar için periyodik sipariş çekme + DB persist.
/// Platform kodu parametre olarak alınır — her platform ayrı Hangfire recurring job.
///
/// Pattern: PullOrdersAsync(son 2 saat) → duplicate check → ExternalOrderDto→Order map → AddAsync → SaveChanges
/// </summary>
[AutomaticRetry(Attempts = 3)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class GenericPlatformOrderSyncJob
{
    private readonly IAdapterFactory _factory;
    private readonly IOrderRepository _orderRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<GenericPlatformOrderSyncJob> _logger;

    public GenericPlatformOrderSyncJob(
        IAdapterFactory factory,
        IOrderRepository orderRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILogger<GenericPlatformOrderSyncJob> logger)
    {
        _factory = factory;
        _orderRepo = orderRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(string platformCode, CancellationToken ct = default)
    {
        _logger.LogInformation("[OrderSync] {Platform} sipariş sync başlıyor...", platformCode);

        var adapter = _factory.ResolveCapability<IOrderCapableAdapter>(platformCode);
        if (adapter is null)
        {
            _logger.LogWarning("[OrderSync] {Platform} IOrderCapableAdapter bulunamadı — atlaniyor", platformCode);
            return;
        }

        try
        {
            var since = DateTime.UtcNow.AddHours(-2);
            var orders = await adapter.PullOrdersAsync(since, ct).ConfigureAwait(false);

            if (orders.Count == 0)
            {
                _logger.LogDebug("[OrderSync] {Platform} yeni sipariş yok (son 2 saat)", platformCode);
                return;
            }

            var tenantId = _tenantProvider.GetCurrentTenantId();
            var platformType = Enum.TryParse<PlatformType>(platformCode, true, out var pt) ? pt : (PlatformType?)null;
            int created = 0, updated = 0, skipped = 0;

            foreach (var dto in orders)
            {
                var existing = await _orderRepo.GetByOrderNumberAsync(dto.OrderNumber, ct).ConfigureAwait(false);
                if (existing is not null)
                {
                    var newStatus = MapStatus(dto.Status);
                    if (existing.Status != newStatus)
                    {
                        existing.Status = newStatus;
                        existing.Notes = $"{platformCode} sync — {dto.Status}";
                        await _orderRepo.UpdateAsync(existing, ct).ConfigureAwait(false);
                        updated++;
                    }
                    else { skipped++; }
                    continue;
                }

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OrderNumber = dto.OrderNumber,
                    ExternalOrderId = dto.PlatformOrderId,
                    PlatformOrderNumber = dto.OrderNumber,
                    SourcePlatform = platformType,
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                    RecipientPhone = dto.CustomerPhone,
                    ShippingAddress = dto.CustomerAddress,
                    OrderDate = dto.OrderDate,
                    Notes = $"{platformCode} sync — {dto.Status}",
                    Status = MapStatus(dto.Status)
                };

                order.SetFinancials(dto.TotalAmount, 0, dto.TotalAmount);
                await _orderRepo.AddAsync(order, ct).ConfigureAwait(false);
                created++;
            }

            if (created > 0 || updated > 0)
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation(
                "[OrderSync] {Platform} TAMAMLANDI — {Total} çekildi, {Created} yeni, {Updated} güncellendi, {Skipped} değişmemiş",
                platformCode, orders.Count, created, updated, skipped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[OrderSync] {Platform} sipariş sync BAŞARISIZ", platformCode);
            throw;
        }
    }

    private static OrderStatus MapStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "CREATED" or "AWAITING" or "NEW" => OrderStatus.Pending,
        "PICKING" or "INVOICED" or "APPROVED" or "PROCESSING" => OrderStatus.Confirmed,
        "SHIPPED" => OrderStatus.Shipped,
        "DELIVERED" or "COMPLETED" => OrderStatus.Delivered,
        "CANCELLED" or "RETURNED" or "REFUNDED" => OrderStatus.Cancelled,
        _ => OrderStatus.Pending
    };
}
