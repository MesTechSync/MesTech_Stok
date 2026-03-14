using Hangfire;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// CRM periyodik job'ları — Hangfire tarafından çalıştırılır.
/// </summary>
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
    public async Task CheckOverdueLeadsAsync()
    {
        _logger.LogInformation("CRM: Süresi geçmiş lead kontrolü başlıyor");
        // Yeni leads 7 günden fazla iletişim olmadıysa uyarı logu
        // TODO: Lead.LastContactedAt threshold kontrolü
        await Task.CompletedTask;
    }

    /// <summary>Her gece 03:00 — süresi geçmiş görevleri işaretle.</summary>
    [DisableConcurrentExecution(60)]
    public async Task CheckOverdueTasksAsync()
    {
        _logger.LogInformation("Task: Süresi geçmiş görev kontrolü başlıyor");
        // Tüm tenant'lar için overdue task'ları bul ve event fırlat
        // ITenantProvider.GetAllTenantIds() gerekir — H28'de genişletilecek
        await Task.CompletedTask;
    }
}
