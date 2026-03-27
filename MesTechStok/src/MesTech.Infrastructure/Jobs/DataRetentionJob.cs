using Hangfire;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Jobs;

/// <summary>
/// KVKK uyumlu kişisel veri saklama süresi denetimi.
/// Her gün gece 03:00'te çalışır. Süresi dolan kişisel verileri anonimleştirir.
/// Silme DEĞİL anonimleştirme — audit trail korunur.
/// </summary>
[AutomaticRetry(Attempts = 2)]
public sealed class DataRetentionJob : ISyncJob
{
    public string JobId => "kvkk-data-retention";
    public string CronExpression => "0 3 * * *"; // Her gün 03:00 UTC

    private readonly AppDbContext _context;
    private readonly ILogger<DataRetentionJob> _logger;

    // KVKK saklama süreleri (gün)
    private const int LoginAttemptRetentionDays = 90;
    private const int AuditLogRetentionDays = 365 * 2; // 2 yıl
    private const int WebhookLogRetentionDays = 180;
    private const int SyncLogRetentionDays = 90;

    public DataRetentionJob(AppDbContext context, ILogger<DataRetentionJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[KVKK] Data retention job started");

        var totalAnonymized = 0;

        // 1. LoginAttempt — IP ve kullanıcı adı anonimleştir
        var loginCutoff = DateTime.UtcNow.AddDays(-LoginAttemptRetentionDays);
        var expiredLogins = await _context.LoginAttempts
            .Where(l => l.CreatedAt < loginCutoff && l.IpAddress != "ANONYMIZED")
            .Take(1000)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var login in expiredLogins)
        {
            login.IpAddress = "ANONYMIZED";
            login.UserAgent = "ANONYMIZED";
            totalAnonymized++;
        }

        // 2. AuditLog — OldValues/NewValues anonimleştir (AuditLog.Timestamp kullanılır, BaseEntity değil)
        var auditCutoff = DateTime.UtcNow.AddDays(-AuditLogRetentionDays);
        var expiredAudits = await _context.AuditLogs
            .Where(a => a.Timestamp < auditCutoff && a.OldValues != "ANONYMIZED")
            .Take(1000)
            .ToListAsync(ct).ConfigureAwait(false);

        // AuditLog immutable (KVKK denetim kayıtları değiştirilemez) — sadece sayı raporu
        var expiredAuditCount = expiredAudits.Count;
        if (expiredAuditCount > 0)
            _logger.LogInformation("[KVKK] {Count} audit log kayıt yaşlandı (2+ yıl) — immutable, arşivlenecek", expiredAuditCount);

        // 3. WebhookLog — payload anonimleştir (Set<T>() ile erişim — DbSet property yok)
        var webhookCutoff = DateTime.UtcNow.AddDays(-WebhookLogRetentionDays);
        var expiredWebhooks = await _context.Set<MesTech.Domain.Entities.WebhookLog>()
            .Where(w => w.ReceivedAt < webhookCutoff && w.Payload != "ANONYMIZED")
            .Take(1000)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var webhook in expiredWebhooks)
        {
            webhook.AnonymizePayload();
            totalAnonymized++;
        }

        // 4. SyncLog — eski kayıtları sil (kişisel veri yok, temizlik)
        var syncCutoff = DateTime.UtcNow.AddDays(-SyncLogRetentionDays);
        var deletedSyncLogs = await _context.SyncLogs
            .Where(s => s.CreatedAt < syncCutoff)
            .Take(5000)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);

        if (totalAnonymized > 0 || deletedSyncLogs > 0)
        {
            await _context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "[KVKK] Data retention completed — Anonymized={Anonymized}, SyncLogsDeleted={Deleted}",
            totalAnonymized, deletedSyncLogs);
    }
}
