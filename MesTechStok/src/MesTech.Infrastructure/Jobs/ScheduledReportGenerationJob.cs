using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Jobs.Accounting;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// Zamanlanmis rapor uretim job'u.
/// Yapilandi irilan rapor zamanlamalarini kontrol eder, rapor uretimini tetikler
/// ve kullaniciya InApp bildirim olusturur.
/// </summary>
[AutomaticRetry(Attempts = 3)]
public class ScheduledReportGenerationJob : IAccountingJob
{
    public string JobId { get; }
    public string CronExpression { get; }

    private readonly ITenantProvider _tenantProvider;
    private readonly IUserNotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScheduledReportGenerationJob> _logger;

    private readonly string _reportType;

    /// <summary>
    /// Dahili rapor turu tanimlari.
    /// </summary>
    private static readonly Dictionary<string, ReportDefinition> ReportDefinitions = new()
    {
        ["daily-sales"] = new ReportDefinition(
            Title: "Gunluk Satis Raporu",
            Message: "Dunkun satis raporu hazir. Siparis, gelir ve kar/zarar ozetini inceleyin.",
            Category: NotificationCategory.Report,
            ActionUrl: "/reports/daily-sales"),

        ["weekly-performance"] = new ReportDefinition(
            Title: "Haftalik Performans Raporu",
            Message: "Gecen haftanin performans raporu hazir. Platform bazli karsilastirma ve trendleri inceleyin.",
            Category: NotificationCategory.Report,
            ActionUrl: "/reports/weekly-performance"),

        ["monthly-financial"] = new ReportDefinition(
            Title: "Aylik Finansal Rapor",
            Message: "Gecen ayin finansal raporu hazir. Kar/zarar, komisyon, vergi ve nakit akis ozetini inceleyin.",
            Category: NotificationCategory.Report,
            ActionUrl: "/reports/monthly-financial")
    };

    public ScheduledReportGenerationJob(
        ITenantProvider tenantProvider,
        IUserNotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ScheduledReportGenerationJob> logger,
        string reportType = "daily-sales")
    {
        _tenantProvider = tenantProvider;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _reportType = reportType;

        // Job ID ve Cron, rapor turune gore belirlenir
        JobId = $"scheduled-report-{reportType}";
        CronExpression = reportType switch
        {
            "daily-sales" => "0 6 * * *",           // Her gun 06:00
            "weekly-performance" => "0 8 * * 1",     // Pazartesi 08:00
            "monthly-financial" => "0 6 1 * *",      // Ayin 1'i 06:00
            _ => "0 6 * * *"
        };
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[{JobId}] Zamanlanmis rapor uretimi basliyor — Tur: {ReportType}",
            JobId, _reportType);

        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();

            if (!ReportDefinitions.TryGetValue(_reportType, out var definition))
            {
                _logger.LogWarning(
                    "[{JobId}] Bilinmeyen rapor turu: {ReportType}", JobId, _reportType);
                return;
            }

            // Rapor uretim simülasyonu — gercek implementasyon MediatR query dispatch eklenecek
            _logger.LogInformation(
                "[{JobId}] {Title} uretiliyor...", JobId, definition.Title);

            // InApp bildirim olustur (tum tenant kullanicilari icin — basit baslangic olarak system user)
            var notification = UserNotification.Create(
                tenantId: tenantId,
                userId: Guid.Empty, // System-level notification — tum kullanicilar gorecek
                title: definition.Title,
                message: $"{definition.Message} ({DateTime.UtcNow:yyyy-MM-dd HH:mm})",
                category: definition.Category,
                actionUrl: definition.ActionUrl);

            await _notificationRepository.AddAsync(notification, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[{JobId}] Rapor bildirimi olusturuldu — Baslik: {Title}, NotificationId: {NotificationId}",
                JobId, definition.Title, notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[{JobId}] Zamanlanmis rapor uretimi HATA — Tur: {ReportType}",
                JobId, _reportType);
            throw;
        }
    }

    /// <summary>
    /// Statik factory — Hangfire'dan parametre gecisi icin kullanilir.
    /// </summary>
    public static async Task ExecuteDailySalesAsync(
        ITenantProvider tenantProvider,
        IUserNotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ScheduledReportGenerationJob> logger,
        CancellationToken ct = default)
    {
        var job = new ScheduledReportGenerationJob(
            tenantProvider, notificationRepository, unitOfWork, logger, "daily-sales");
        await job.ExecuteAsync(ct);
    }

    public static async Task ExecuteWeeklyPerformanceAsync(
        ITenantProvider tenantProvider,
        IUserNotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ScheduledReportGenerationJob> logger,
        CancellationToken ct = default)
    {
        var job = new ScheduledReportGenerationJob(
            tenantProvider, notificationRepository, unitOfWork, logger, "weekly-performance");
        await job.ExecuteAsync(ct);
    }

    public static async Task ExecuteMonthlyFinancialAsync(
        ITenantProvider tenantProvider,
        IUserNotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ScheduledReportGenerationJob> logger,
        CancellationToken ct = default)
    {
        var job = new ScheduledReportGenerationJob(
            tenantProvider, notificationRepository, unitOfWork, logger, "monthly-financial");
        await job.ExecuteAsync(ct);
    }

    private record ReportDefinition(
        string Title,
        string Message,
        NotificationCategory Category,
        string ActionUrl);
}
