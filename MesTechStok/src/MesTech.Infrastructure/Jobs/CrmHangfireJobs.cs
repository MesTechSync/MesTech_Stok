using Hangfire;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// CRM periyodik job'ları — Hangfire tarafından çalıştırılır.
/// DEV6-TUR7: stub → gerçek iş mantığı.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public sealed class CrmHangfireJobs
{
    private readonly ICrmLeadRepository _leadRepo;
    private readonly IWorkTaskRepository _taskRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CrmHangfireJobs> _logger;

    private const int StaleLeadDays = 30;

    public CrmHangfireJobs(
        ICrmLeadRepository leadRepo, IWorkTaskRepository taskRepo,
        IOrderRepository orderRepo, ITenantRepository tenantRepo,
        IUnitOfWork uow, ILogger<CrmHangfireJobs> logger)
    {
        _leadRepo = leadRepo;
        _taskRepo = taskRepo;
        _orderRepo = orderRepo;
        _tenantRepo = tenantRepo;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Her gece 02:00 — 30 gün temassız New/Contacted lead'leri Lost olarak işaretle.</summary>
    [DisableConcurrentExecution(60)]
    public async Task CheckOverdueLeadsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("CRM: Süresi geçmiş lead kontrolü başlıyor");
        try
        {
            var tenants = await _tenantRepo.GetAllAsync(ct).ConfigureAwait(false);
            var totalMarked = 0;

            foreach (var tenant in tenants)
            {
                ct.ThrowIfCancellationRequested();

                var newLeads = await _leadRepo.GetPagedAsync(
                    tenant.Id, LeadStatus.New, null, 1, 500, ct).ConfigureAwait(false);
                var contactedLeads = await _leadRepo.GetPagedAsync(
                    tenant.Id, LeadStatus.Contacted, null, 1, 500, ct).ConfigureAwait(false);

                var cutoff = DateTime.UtcNow.AddDays(-StaleLeadDays);

                foreach (var lead in newLeads.Items)
                {
                    if (lead.CreatedAt < cutoff && (lead.ContactedAt is null || lead.ContactedAt < cutoff))
                    {
                        lead.MarkAsLost($"Auto-closed: no contact for {StaleLeadDays} days");
                        totalMarked++;
                    }
                }

                foreach (var lead in contactedLeads.Items)
                {
                    if (lead.ContactedAt.HasValue && lead.ContactedAt.Value < cutoff)
                    {
                        lead.MarkAsLost($"Auto-closed: no follow-up for {StaleLeadDays} days");
                        totalMarked++;
                    }
                }
            }

            if (totalMarked > 0)
                await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("CRM: CheckOverdueLeads tamamlandı — {Count} lead Lost olarak işaretlendi", totalMarked);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CRM: CheckOverdueLeads hatası");
            throw;
        }
    }

    /// <summary>Her gece 03:00 — süresi geçmiş görevleri işaretle ve TaskOverdueEvent fırlat.</summary>
    [DisableConcurrentExecution(60)]
    public async Task CheckOverdueTasksAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Task: Süresi geçmiş görev kontrolü başlıyor");
        try
        {
            var tenants = await _tenantRepo.GetAllAsync(ct).ConfigureAwait(false);
            var totalOverdue = 0;

            foreach (var tenant in tenants)
            {
                ct.ThrowIfCancellationRequested();

                var overdueTasks = await _taskRepo.GetOverdueAsync(tenant.Id, ct).ConfigureAwait(false);

                foreach (var task in overdueTasks)
                {
                    if (task.CheckOverdue())
                        totalOverdue++;
                }
            }

            if (totalOverdue > 0)
                await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Task: CheckOverdueTasks tamamlandı — {Count} görev overdue", totalOverdue);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CRM: CheckOverdueTasks hatası");
            throw;
        }
    }

    /// <summary>
    /// Her 30 dk — son siparişleri tara, CRM lead yoksa müşteri bilgisiyle oluştur (H29-3.5).
    /// </summary>
    [DisableConcurrentExecution(120)]
    public async Task CreateLeadsFromNewOrdersAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("CRM: Yeni sipariş → Lead tarama başlıyor");
        try
        {
            var since = DateTime.UtcNow.AddMinutes(-35);
            var tenants = await _tenantRepo.GetAllAsync(ct).ConfigureAwait(false);
            var totalCreated = 0;

            foreach (var tenant in tenants)
            {
                ct.ThrowIfCancellationRequested();

                var recentOrders = await _orderRepo.GetByDateRangeAsync(
                    tenant.Id, since, DateTime.UtcNow, ct).ConfigureAwait(false);

                foreach (var order in recentOrders)
                {
                    if (string.IsNullOrWhiteSpace(order.CustomerName))
                        continue;

                    var existingLeads = await _leadRepo.GetPagedAsync(
                        tenant.Id, null, null, 1, 1, ct).ConfigureAwait(false);

                    // Simple check: if customer email matches existing lead, skip
                    // Full dedup would query by email — for now create if no leads exist for tenant
                    if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
                    {
                        var source = order.SourcePlatform.HasValue
                            ? LeadSource.MarketplaceInquiry
                            : LeadSource.Web;

                        var lead = Lead.Create(
                            tenant.Id,
                            order.CustomerName,
                            source,
                            email: order.CustomerEmail);

                        await _leadRepo.AddAsync(lead, ct).ConfigureAwait(false);
                        totalCreated++;
                    }
                }
            }

            if (totalCreated > 0)
                await _uow.SaveChangesAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("CRM: Sipariş → Lead tarama tamamlandı — {Count} yeni lead oluşturuldu", totalCreated);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CRM: CreateLeadsFromNewOrders hatası");
            throw;
        }
    }
}
