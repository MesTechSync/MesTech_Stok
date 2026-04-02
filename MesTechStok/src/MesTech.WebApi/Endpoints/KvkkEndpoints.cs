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
        .WithSummary("KVKK madde 7 — kişisel veri silme (anonimizasyon) talebi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

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
                new GetKvkkAuditLogsQuery(tenantId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("KvkkGetAuditLogs")
        .WithSummary("KVKK madde 12 — denetim kayıtları (yasal saklama 10 yıl)").Produces(200).Produces(400)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/kvkk/rights — KVKK hakları bilgi endpoint'i
        group.MapGet("/rights", () =>
        {
            return Results.Ok(new KvkkRightsResponse(
                "6698 sayılı Kişisel Verilerin Korunması Kanunu",
                new[]
                {
                    new KvkkRightItem("10", "Aydınlatma yükümlülüğü", "GET /api/v1/kvkk/rights"),
                    new KvkkRightItem("11/a", "Kişisel verinin işlenip işlenmediğini öğrenme", "GET /api/v1/kvkk/export"),
                    new KvkkRightItem("11/b", "İşlenmiş ise bilgi talep etme", "GET /api/v1/kvkk/export"),
                    new KvkkRightItem("11/c", "Verilerin aktarıldığı üçüncü kişileri bilme", "GET /api/v1/kvkk/processors"),
                    new KvkkRightItem("11/ç", "Eksik/yanlış verilerin düzeltilmesini isteme", "PUT /api/v1/settings/profile (mevcut)"),
                    new KvkkRightItem("11/d", "Verilerin silinmesini/yok edilmesini isteme", "POST /api/v1/kvkk/delete"),
                    new KvkkRightItem("11/e", "Düzeltme/silmenin üçüncü kişilere bildirilmesini isteme", "Otomatik — audit log kaydı"),
                    new KvkkRightItem("11/f", "Otomatik sistemlerle aleyhte karar çıkmasına itiraz", "İletişim: kvkk@mestech.com.tr"),
                    new KvkkRightItem("11/g", "Zararın giderilmesini talep etme", "İletişim: kvkk@mestech.com.tr")
                },
                new[]
                {
                    new KvkkDataProcessorItem("PostgreSQL (Self-Hosted)", "Ana veritabanı", "TR"),
                    new KvkkDataProcessorItem("Redis (Self-Hosted)", "Önbellek", "TR"),
                    new KvkkDataProcessorItem("RabbitMQ (Self-Hosted)", "Mesaj kuyruğu", "TR"),
                    new KvkkDataProcessorItem("MinIO (Self-Hosted)", "Dosya depolama", "TR"),
                    new KvkkDataProcessorItem("Iyzico/PayTR", "Ödeme işleme", "TR"),
                    new KvkkDataProcessorItem("Trendyol/HB/N11/Amazon API", "Pazaryeri entegrasyonu", "TR/EU")
                },
                new KvkkRetentionPolicyInfo(
                    "10 yıl (TTK madde 82, VUK madde 253)",
                    "10 yıl (yasal zorunluluk)",
                    "Hesap kapatılana kadar + 30 gün",
                    "90 gün rotasyon"),
                "kvkk@mestech.com.tr",
                "2026-03-26"));
        })
        .WithName("KvkkRights")
        .WithSummary("KVKK hakları bilgilendirme — madde 10 aydınlatma yükümlülüğü").Produces(200).Produces(400)
        .CacheOutput("Lookup60s");

        // GET /api/v1/kvkk/processors — veri işleyen üçüncü taraf listesi
        group.MapGet("/processors", () =>
        {
            return Results.Ok(new KvkkProcessorsResponse(
                new[]
                {
                    new KvkkProcessorDetailItem("PostgreSQL", "Database", true, "TR", "N/A (self-hosted)"),
                    new KvkkProcessorDetailItem("Redis", "Cache", true, "TR", "N/A (self-hosted)"),
                    new KvkkProcessorDetailItem("RabbitMQ", "Message Broker", true, "TR", "N/A (self-hosted)"),
                    new KvkkProcessorDetailItem("MinIO", "Object Storage", true, "TR", "N/A (self-hosted)"),
                    new KvkkProcessorDetailItem("Iyzico", "Payment Gateway", false, "TR", "DPA signed"),
                    new KvkkProcessorDetailItem("PayTR", "Payment Gateway", false, "TR", "DPA signed"),
                    new KvkkProcessorDetailItem("Stripe", "Payment Gateway", false, "EU/US", "SCCs applied"),
                    new KvkkProcessorDetailItem("Trendyol API", "Marketplace Integration", false, "TR", "DPA signed"),
                    new KvkkProcessorDetailItem("Hepsiburada API", "Marketplace Integration", false, "TR", "DPA signed"),
                    new KvkkProcessorDetailItem("Amazon SP-API", "Marketplace Integration", false, "EU", "SCCs applied")
                },
                "2026-03-26",
                "2026-06-26"));
        })
        .WithName("KvkkProcessors")
        .WithSummary("KVKK — veri işleyen üçüncü taraf listesi (madde 11/c)").Produces(200).Produces(400)
        .CacheOutput("Lookup60s");
    }

    public sealed record KvkkRightItem(string Article, string Right, string Endpoint);
    public sealed record KvkkDataProcessorItem(string Name, string Purpose, string Location);
    public sealed record KvkkRetentionPolicyInfo(
        string TransactionalData, string KvkkAuditLogs, string UserData, string BackupData);
    public sealed record KvkkRightsResponse(
        string Law, IReadOnlyList<KvkkRightItem> Rights, IReadOnlyList<KvkkDataProcessorItem> DataProcessors,
        KvkkRetentionPolicyInfo RetentionPolicy, string Contact, string LastUpdated);

    public sealed record KvkkProcessorDetailItem(
        string Name, string Type, bool SelfHosted, string Location, string Gdpr);
    public sealed record KvkkProcessorsResponse(
        IReadOnlyList<KvkkProcessorDetailItem> DataProcessors, string LastAuditDate, string NextAuditDate);
}
