using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Her saat çalışır. 48+ saat gönderilmemiş siparişleri bulur.
/// Zincir 11: Hangfire → GetStaleOrdersQuery → bildirim dispatch.
///
/// Tüm tenant'lar için kontrol yapar (ITenantRepository.GetAllAsync).
/// Ciddiyet bazlı: 72+ saat = Kritik, 48-72 saat = Uyarı.
/// </summary>
[AutomaticRetry(Attempts = 2)]
public class CheckStaleOrdersJob
{
    private readonly IMediator _mediator;
    private readonly ITenantRepository _tenantRepo;
    private readonly ILogger<CheckStaleOrdersJob> _logger;

    public CheckStaleOrdersJob(
        IMediator mediator,
        ITenantRepository tenantRepo,
        ILogger<CheckStaleOrdersJob> logger)
    {
        _mediator = mediator;
        _tenantRepo = tenantRepo;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[StaleOrders] Gecikmiş sipariş kontrolü başlıyor...");

        var tenants = await _tenantRepo.GetAllAsync(ct).ConfigureAwait(false);

        foreach (var tenant in tenants)
        {
            try
            {
                var staleOrders = await _mediator.Send(
                    new GetStaleOrdersQuery(tenant.Id), ct).ConfigureAwait(false);

                if (!staleOrders.Any()) continue;

                _logger.LogWarning(
                    "[StaleOrders] Tenant {TenantId}: {Count} gecikmiş sipariş!",
                    tenant.Id, staleOrders.Count);

                var critical = staleOrders.Where(o => o.Elapsed.TotalHours > 72).ToList();
                var warning = staleOrders.Where(o => o.Elapsed.TotalHours <= 72).ToList();

                if (critical.Count > 0)
                {
                    await _mediator.Send(new SendNotificationCommand(
                        TenantId: tenant.Id,
                        Channel: "System",
                        Recipient: "tenant-admins",
                        TemplateName: "stale-orders-critical",
                        Content: $"🚨 {critical.Count} SİPARİŞ 72+ SAAT GECİKMİŞ!\n" +
                                 string.Join("\n", critical.Select(o =>
                                     $"• #{o.OrderNumber} ({o.Platform}) — {o.Elapsed.TotalHours:F0}h"))
                    ), ct).ConfigureAwait(false);
                }

                if (warning.Count > 0)
                {
                    await _mediator.Send(new SendNotificationCommand(
                        TenantId: tenant.Id,
                        Channel: "System",
                        Recipient: "tenant-admins",
                        TemplateName: "stale-orders-warning",
                        Content: $"⚠️ {warning.Count} sipariş 48+ saat gönderilmemiş\n" +
                                 string.Join("\n", warning.Select(o =>
                                     $"• #{o.OrderNumber} ({o.Platform}) — {o.Elapsed.TotalHours:F0}h"))
                    ), ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[StaleOrders] Tenant {TenantId} kontrolü hatası", tenant.Id);
            }
        }

        _logger.LogInformation("[StaleOrders] Gecikmiş sipariş kontrolü tamamlandı.");
    }
}
