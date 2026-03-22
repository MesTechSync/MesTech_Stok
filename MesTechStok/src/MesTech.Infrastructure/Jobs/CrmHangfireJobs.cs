using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// CRM periyodik job'ları — Hangfire tarafından çalıştırılır.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class CrmHangfireJobs
{
    private readonly ICrmLeadRepository _leadRepo;
    private readonly IWorkTaskRepository _taskRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CrmHangfireJobs> _logger;

    public CrmHangfireJobs(
        ICrmLeadRepository leadRepo, IWorkTaskRepository taskRepo,
        IUnitOfWork uow, ILogger<CrmHangfireJobs> logger)
    {
        _leadRepo = leadRepo;
        _taskRepo = taskRepo;
        _uow = uow;
        _logger = logger;
    }

    /// <summary>Her gece 02:00 — süresi geçmiş lead'leri işaretle.</summary>
    [DisableConcurrentExecution(60)]
    public async Task CheckOverdueLeadsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("CRM: Süresi geçmiş lead kontrolü başlıyor");
        try
        {
            // Future: Lead.LastContactedAt threshold kontrolü
            ct.ThrowIfCancellationRequested();
            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CRM: CheckOverdueLeads hatası");
            throw;
        }
    }

    /// <summary>Her gece 03:00 — süresi geçmiş görevleri işaretle.</summary>
    [DisableConcurrentExecution(60)]
    public async Task CheckOverdueTasksAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Task: Süresi geçmiş görev kontrolü başlıyor");
        try
        {
            // Future: ITenantProvider.GetAllTenantIds() ile overdue task tarama
            ct.ThrowIfCancellationRequested();
            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CRM: CheckOverdueTasks hatası");
            throw;
        }
    }

    /// <summary>
    /// Her 30 dk — son siparişleri tara, CRM lead/contact yoksa oluştur (H29-3.5).
    /// </summary>
    [DisableConcurrentExecution(120)]
    public async Task CreateLeadsFromNewOrdersAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("CRM: Yeni sipariş → Lead tarama başlıyor");
        try
        {
            var since = DateTime.UtcNow.AddMinutes(-35);
            var tenantId = new Guid("00000000-0000-0000-0000-000000000001");

            // Future: await _crmBridgeService.CreateLeadsFromRecentOrdersAsync(tenantId, since, ct);
            ct.ThrowIfCancellationRequested();
            await Task.CompletedTask.ConfigureAwait(false);
            _logger.LogInformation("CRM: Sipariş → Lead tarama tamamlandı");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "CRM: CreateLeadsFromNewOrders hatası");
            throw;
        }
    }
}
