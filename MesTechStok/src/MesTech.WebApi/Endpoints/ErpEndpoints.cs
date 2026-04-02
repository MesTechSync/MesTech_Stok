using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Erp.Commands.CreateErpAccountMapping;
using MesTech.Application.Features.Erp.Commands.DeleteErpAccountMapping;
using MesTech.Application.Features.Erp.Commands.SyncOrderToErp;
using MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;
using MesTech.Application.Features.Erp.Queries.GetErpSyncHistory;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// ERP integration endpoints — provider listing, connection testing, sync triggers.
/// </summary>
public static class ErpEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/erp").WithTags("ERP").RequireRateLimiting("PerApiKey");

        // GET /api/v1/erp/providers — list all registered ERP providers
        group.MapGet("/providers", (IErpAdapterFactory factory) =>
        {
            var providers = factory.SupportedProviders
                .Select(p => new ErpProviderItem(p.ToString(), (int)p))
                .ToList();

            return Results.Ok(ApiResponse<ErpProviderListResponse>.Ok(
                new ErpProviderListResponse(providers, providers.Count)));
        })
        .WithName("GetErpProviders")
        .WithSummary("Kayıtlı ERP sağlayıcı listesi")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/erp/status — ping all registered ERP adapters
        group.MapGet("/status", async (IErpAdapterFactory factory, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            var statuses = new List<ErpStatusItem>();

            foreach (var provider in factory.SupportedProviders)
            {
                var adapter = factory.GetAdapter(provider);
                bool isAlive;

#pragma warning disable CA1031 // Intentional: status check must not throw
                try
                {
                    isAlive = await adapter.PingAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "ERP ping failed for provider {Provider}", provider);
                    isAlive = false;
                }
#pragma warning restore CA1031

                statuses.Add(new ErpStatusItem(
                    provider.ToString(), (int)provider, isAlive, DateTime.UtcNow));
            }

            return Results.Ok(ApiResponse<ErpStatusListResponse>.Ok(
                new ErpStatusListResponse(statuses, DateTime.UtcNow)));
        })
        .WithName("GetErpStatus")
        .WithSummary("Tüm ERP adapter'larını ping — bağlantı durumu")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // POST /api/v1/erp/test-connection — test connection to a specific ERP provider
        group.MapPost("/test-connection", async (
            ErpTestConnectionRequest request,
            IErpAdapterFactory factory,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            if (!Enum.TryParse<ErpProvider>(request.Provider, ignoreCase: true, out var provider)
                || provider == ErpProvider.None)
            {
                return Results.BadRequest(ApiResponse<object>.Fail($"Invalid provider: '{request.Provider}'", "INVALID_PROVIDER"));
            }

#pragma warning disable CA1031 // Intentional: connection test must return result, not throw
            try
            {
                var adapter = factory.GetAdapter(provider);
                var isAlive = await adapter.PingAsync(ct);

                return Results.Ok(new ErpConnectionTestResponse(
                    provider.ToString(), isAlive, null, DateTime.UtcNow));
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Gecersiz ERP provider parametresi.", "INVALID_PROVIDER"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERP connection test failed for {Provider}", provider);
                return Results.Ok(new ErpConnectionTestResponse(
                    provider.ToString(), false,
                    "Baglanti testi basarisiz — detaylar server log'unda.",
                    DateTime.UtcNow));
            }
#pragma warning restore CA1031
        })
        .WithName("TestErpConnection")
        .WithSummary("Belirli ERP sağlayıcısına bağlantı testi").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/erp/sync/stock — trigger manual ERP stock sync
        group.MapPost("/sync/stock", async (
            IErpAdapterFactory factory,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            logger.LogInformation("[ErpEndpoints] Manual ERP stock sync triggered");

            var results = new List<ErpSyncResultItem>();

            foreach (var provider in factory.SupportedProviders)
            {
#pragma warning disable CA1031 // Intentional: per-adapter isolation
                try
                {
                    var adapter = factory.GetAdapter(provider);
                    if (adapter is IErpStockCapable stockCapable)
                    {
                        var items = await stockCapable.GetStockLevelsAsync(ct);
                        results.Add(new ErpSyncResultItem(
                            provider.ToString(), true, items.Count, null));
                    }
                    else
                    {
                        results.Add(new ErpSyncResultItem(
                            provider.ToString(), false, 0, "Not stock-capable"));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ERP stock sync failed for {Provider}", provider);
                    results.Add(new ErpSyncResultItem(
                        provider.ToString(), false, 0,
                        "Stok senkronizasyonu basarisiz — detaylar server log'unda."));
                }
#pragma warning restore CA1031
            }

            return Results.Ok(ApiResponse<ErpSyncResponse>.Ok(
                new ErpSyncResponse(results, DateTime.UtcNow)));
        })
        .WithName("SyncErpStock")
        .WithSummary("Manuel ERP stok senkronizasyonu tetikle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/erp/sync/accounts — trigger manual ERP account sync
        group.MapPost("/sync/accounts", async (
            IErpAdapterFactory factory,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            logger.LogInformation("[ErpEndpoints] Manual ERP account sync triggered");

            var accountResults = new List<ErpAccountSyncItem>();

            foreach (var provider in factory.SupportedProviders)
            {
#pragma warning disable CA1031 // Intentional: per-adapter isolation
                try
                {
                    var adapter = factory.GetAdapter(provider);
                    var accounts = await adapter.GetAccountBalancesAsync(ct);
                    accountResults.Add(new ErpAccountSyncItem(
                        provider.ToString(), true, accounts.Count,
                        accounts.Sum(a => a.Balance), null));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ERP account sync failed for {Provider}", provider);
                    accountResults.Add(new ErpAccountSyncItem(
                        provider.ToString(), false, 0, 0,
                        "Hesap senkronizasyonu basarisiz — detaylar server log'unda."));
                }
#pragma warning restore CA1031
            }

            return Results.Ok(ApiResponse<ErpAccountSyncResponse>.Ok(
                new ErpAccountSyncResponse(accountResults, DateTime.UtcNow)));
        })
        .WithName("SyncErpAccounts")
        .WithSummary("Manuel ERP cari hesap senkronizasyonu tetikle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/erp/sync/history — ERP senkronizasyon geçmişi
        group.MapGet("/sync/history", async (
            Guid tenantId,
            int? page,
            int? pageSize,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetErpSyncHistoryQuery(tenantId, page ?? 1, Math.Clamp(pageSize ?? 20, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpSyncHistory")
        .WithSummary("ERP senkronizasyon geçmişi")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/erp/dashboard — ERP dashboard özeti
        group.MapGet("/dashboard", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetErpDashboardQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpDashboard")
        .WithSummary("ERP dashboard — bağlantı durumu, son sync, özet")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/erp/sync/logs — ERP sync log listesi
        group.MapGet("/sync/logs", async (
            Guid tenantId, int? page, int? pageSize,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetErpSyncLogsQuery(tenantId, page ?? 1, Math.Clamp(pageSize ?? 20, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpSyncLogs")
        .WithSummary("ERP senkronizasyon log listesi")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/erp/sync-order — siparişi ERP'ye senkronize et
        group.MapPost("/sync-order", async (
            SyncOrderToErpCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.Success
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("SyncOrderToErp")
        .WithSummary("Siparişi ERP sistemine senkronize et (Parasüt, Logo, vb.)")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
        // GET /api/v1/erp/account-mappings — ERP hesap eşleştirme listesi
        group.MapGet("/account-mappings", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetErpAccountMappingsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpAccountMappings")
        .WithSummary("ERP hesap eşleştirme listesi — MesTech ↔ ERP hesap kodu mapping")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/erp/account-mappings — yeni ERP hesap eşleştirmesi oluştur
        group.MapPost("/account-mappings", async (
            CreateErpAccountMappingCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/erp/account-mappings/{result.MappingId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateErpAccountMapping")
        .WithSummary("Yeni ERP hesap eşleştirmesi oluştur — MesTech hesabını ERP hesabına bağla")
        .Produces(201).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/erp/account-mappings/{id} — ERP hesap eşleştirmesini sil
        group.MapDelete("/account-mappings/{id:guid}", async (
            Guid id, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteErpAccountMappingCommand(tenantId, id), ct);
            return result
                ? Results.NoContent()
                : Results.NotFound(ApiResponse<object>.Fail("Eşleştirme bulunamadı", "NOT_FOUND"));
        })
        .WithName("DeleteErpAccountMapping")
        .WithSummary("ERP hesap eşleştirmesini sil")
        .Produces(204).Produces(404);
    }

    // ── Request DTOs ──────────────────────────────────────────────────

    private record ErpTestConnectionRequest(string Provider);

    public sealed record ErpProviderItem(string Provider, int Id);
    public sealed record ErpProviderListResponse(IReadOnlyList<ErpProviderItem> Providers, int Count);

    public sealed record ErpStatusItem(string Provider, int Id, bool Connected, DateTime CheckedAt);
    public sealed record ErpStatusListResponse(IReadOnlyList<ErpStatusItem> Statuses, DateTime Timestamp);

    public sealed record ErpConnectionTestResponse(
        string Provider, bool Connected, string? Error, DateTime TestedAt);

    public sealed record ErpSyncResultItem(string Provider, bool Success, int ItemCount, string? Reason);
    public sealed record ErpSyncResponse(IReadOnlyList<ErpSyncResultItem> Results, DateTime TriggeredAt);

    public sealed record ErpAccountSyncItem(
        string Provider, bool Success, int AccountCount, decimal TotalBalance, string? Reason);
    public sealed record ErpAccountSyncResponse(
        IReadOnlyList<ErpAccountSyncItem> Results, DateTime TriggeredAt);
}
