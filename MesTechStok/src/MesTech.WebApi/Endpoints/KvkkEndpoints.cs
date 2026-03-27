using MediatR;
using MesTech.Application.Features.System.Kvkk.Commands.DeletePersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.ExportPersonalData;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// KVKK/GDPR kişisel veri hakları endpoint'leri — 6698 sayılı KVKK uyumlu.
/// Veri silme (madde 7), dışa aktarma (madde 11/c), denetim kaydı (madde 12).
/// </summary>
public static class KvkkEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/kvkk")
            .WithTags("KVKK — Kişisel Veri Hakları")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/kvkk/delete — kişisel veri silme talebi (KVKK madde 7)
        group.MapPost("/delete", async (
            DeletePersonalDataCommand command,
            IKvkkAuditLogRepository auditRepo,
            IUnitOfWork uow,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);

            // KVKK denetim kaydı — yasal zorunluluk (10 yıl saklama)
            var auditLog = KvkkAuditLog.Create(
                command.TenantId,
                command.RequestedByUserId,
                KvkkOperationType.DataAnonymization,
                command.Reason,
                result.AnonymizedRecords,
                result.Success,
                details: $"Anonymized {result.AnonymizedRecords} records at {result.ProcessedAt:O}");
            await auditRepo.AddAsync(auditLog, ct);
            await uow.SaveChangesAsync(ct);

            return Results.Ok(result);
        })
        .WithName("KvkkDeletePersonalData")
        .WithSummary("KVKK madde 7 — kişisel veri silme (anonimizasyon) talebi").Produces(200).Produces(400);

        // GET /api/v1/kvkk/export — kişisel veri dışa aktarma (KVKK madde 11/c)
        group.MapGet("/export", async (
            Guid tenantId, Guid requestedByUserId,
            IKvkkAuditLogRepository auditRepo,
            IUnitOfWork uow,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ExportPersonalDataQuery(tenantId, requestedByUserId), ct);

            // KVKK denetim kaydı
            var auditLog = KvkkAuditLog.Create(
                tenantId,
                requestedByUserId,
                KvkkOperationType.DataExport,
                "KVKK madde 11/c — veri taşınabilirlik hakkı",
                result.UserCount + result.StoreCount + result.OrderCount,
                isSuccess: true,
                details: $"Export: {result.UserCount} users, {result.StoreCount} stores, {result.OrderCount} orders, {result.ProductCount} products");
            await auditRepo.AddAsync(auditLog, ct);
            await uow.SaveChangesAsync(ct);

            return Results.Ok(result);
        })
        .WithName("KvkkExportPersonalData")
        .WithSummary("KVKK madde 11/c — kişisel veri dışa aktarma (JSON)").Produces(200).Produces(400);

        // GET /api/v1/kvkk/export/download — veri dışa aktarma dosya indirme
        group.MapGet("/export/download", async (
            Guid tenantId, Guid requestedByUserId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ExportPersonalDataQuery(tenantId, requestedByUserId), ct);

            var bytes = System.Text.Encoding.UTF8.GetBytes(result.DataJson);
            return Results.File(
                bytes,
                "application/json",
                $"kvkk_export_{tenantId:N}_{DateTime.UtcNow:yyyyMMdd}.json");
        })
        .WithName("KvkkExportDownload")
        .WithSummary("KVKK — kişisel veri export dosyası indirme (JSON)").Produces(200).Produces(400);

        // GET /api/v1/kvkk/audit-logs — KVKK denetim kayıtları
        group.MapGet("/audit-logs", async (
            Guid tenantId, int page = 1, int pageSize = 20,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetKvkkAuditLogsQuery(tenantId, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("KvkkGetAuditLogs")
        .WithSummary("KVKK madde 12 — denetim kayıtları (yasal saklama 10 yıl)").Produces(200).Produces(400);

        // GET /api/v1/kvkk/rights — KVKK hakları bilgi endpoint'i
        group.MapGet("/rights", () =>
        {
            return Results.Ok(new
            {
                Law = "6698 sayılı Kişisel Verilerin Korunması Kanunu",
                Rights = new[]
                {
                    new { Article = "10", Right = "Aydınlatma yükümlülüğü", Endpoint = "GET /api/v1/kvkk/rights" },
                    new { Article = "11/a", Right = "Kişisel verinin işlenip işlenmediğini öğrenme", Endpoint = "GET /api/v1/kvkk/export" },
                    new { Article = "11/b", Right = "İşlenmiş ise bilgi talep etme", Endpoint = "GET /api/v1/kvkk/export" },
                    new { Article = "11/c", Right = "Verilerin aktarıldığı üçüncü kişileri bilme", Endpoint = "GET /api/v1/kvkk/processors" },
                    new { Article = "11/ç", Right = "Eksik/yanlış verilerin düzeltilmesini isteme", Endpoint = "PUT /api/v1/settings/profile (mevcut)" },
                    new { Article = "11/d", Right = "Verilerin silinmesini/yok edilmesini isteme", Endpoint = "POST /api/v1/kvkk/delete" },
                    new { Article = "11/e", Right = "Düzeltme/silmenin üçüncü kişilere bildirilmesini isteme", Endpoint = "Otomatik — audit log kaydı" },
                    new { Article = "11/f", Right = "Otomatik sistemlerle aleyhte karar çıkmasına itiraz", Endpoint = "İletişim: kvkk@mestech.com.tr" },
                    new { Article = "11/g", Right = "Zararın giderilmesini talep etme", Endpoint = "İletişim: kvkk@mestech.com.tr" }
                },
                DataProcessors = new[]
                {
                    new { Name = "PostgreSQL (Self-Hosted)", Purpose = "Ana veritabanı", Location = "TR" },
                    new { Name = "Redis (Self-Hosted)", Purpose = "Önbellek", Location = "TR" },
                    new { Name = "RabbitMQ (Self-Hosted)", Purpose = "Mesaj kuyruğu", Location = "TR" },
                    new { Name = "MinIO (Self-Hosted)", Purpose = "Dosya depolama", Location = "TR" },
                    new { Name = "Iyzico/PayTR", Purpose = "Ödeme işleme", Location = "TR" },
                    new { Name = "Trendyol/HB/N11/Amazon API", Purpose = "Pazaryeri entegrasyonu", Location = "TR/EU" }
                },
                RetentionPolicy = new
                {
                    TransactionalData = "10 yıl (TTK madde 82, VUK madde 253)",
                    KvkkAuditLogs = "10 yıl (yasal zorunluluk)",
                    UserData = "Hesap kapatılana kadar + 30 gün",
                    BackupData = "90 gün rotasyon"
                },
                Contact = "kvkk@mestech.com.tr",
                LastUpdated = "2026-03-26"
            });
        })
        .WithName("KvkkRights")
        .WithSummary("KVKK hakları bilgilendirme — madde 10 aydınlatma yükümlülüğü").Produces(200).Produces(400);

        // GET /api/v1/kvkk/processors — veri işleyen üçüncü taraf listesi
        group.MapGet("/processors", () =>
        {
            return Results.Ok(new
            {
                DataProcessors = new[]
                {
                    new { Name = "PostgreSQL", Type = "Database", SelfHosted = true, Location = "TR", Gdpr = "N/A (self-hosted)" },
                    new { Name = "Redis", Type = "Cache", SelfHosted = true, Location = "TR", Gdpr = "N/A (self-hosted)" },
                    new { Name = "RabbitMQ", Type = "Message Broker", SelfHosted = true, Location = "TR", Gdpr = "N/A (self-hosted)" },
                    new { Name = "MinIO", Type = "Object Storage", SelfHosted = true, Location = "TR", Gdpr = "N/A (self-hosted)" },
                    new { Name = "Iyzico", Type = "Payment Gateway", SelfHosted = false, Location = "TR", Gdpr = "DPA signed" },
                    new { Name = "PayTR", Type = "Payment Gateway", SelfHosted = false, Location = "TR", Gdpr = "DPA signed" },
                    new { Name = "Stripe", Type = "Payment Gateway", SelfHosted = false, Location = "EU/US", Gdpr = "SCCs applied" },
                    new { Name = "Trendyol API", Type = "Marketplace Integration", SelfHosted = false, Location = "TR", Gdpr = "DPA signed" },
                    new { Name = "Hepsiburada API", Type = "Marketplace Integration", SelfHosted = false, Location = "TR", Gdpr = "DPA signed" },
                    new { Name = "Amazon SP-API", Type = "Marketplace Integration", SelfHosted = false, Location = "EU", Gdpr = "SCCs applied" }
                },
                LastAuditDate = "2026-03-26",
                NextAuditDate = "2026-06-26"
            });
        })
        .WithName("KvkkProcessors")
        .WithSummary("KVKK — veri işleyen üçüncü taraf listesi (madde 11/c)").Produces(200).Produces(400);
    }
}
