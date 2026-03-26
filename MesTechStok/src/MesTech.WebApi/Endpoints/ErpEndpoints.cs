using MediatR;
using MesTech.Application.DTOs;
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
                .Select(p => new
                {
                    provider = p.ToString(),
                    id = (int)p
                })
                .ToList();

            return Results.Ok(ApiResponse<object>.Ok(new { providers, count = providers.Count }));
        })
        .WithName("GetErpProviders")
        .WithSummary("Kayıtlı ERP sağlayıcı listesi")
        .CacheOutput("Lookup60s");

        // GET /api/v1/erp/status — ping all registered ERP adapters
        group.MapGet("/status", async (IErpAdapterFactory factory, ILoggerFactory loggerFactory, CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            var statuses = new List<object>();

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

                statuses.Add(new
                {
                    provider = provider.ToString(),
                    id = (int)provider,
                    connected = isAlive,
                    checkedAt = DateTime.UtcNow
                });
            }

            return Results.Ok(ApiResponse<object>.Ok(new { statuses, timestamp = DateTime.UtcNow }));
        })
        .WithName("GetErpStatus")
        .WithSummary("Tüm ERP adapter'larını ping — bağlantı durumu")
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

                return Results.Ok(new
                {
                    provider = provider.ToString(),
                    connected = isAlive,
                    testedAt = DateTime.UtcNow
                });
            }
            catch (ArgumentException)
            {
                return Results.BadRequest(ApiResponse<object>.Fail("Gecersiz ERP provider parametresi.", "INVALID_PROVIDER"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERP connection test failed for {Provider}", provider);
                return Results.Ok(new
                {
                    provider = provider.ToString(),
                    connected = false,
                    error = "Baglanti testi basarisiz — detaylar server log'unda.",
                    testedAt = DateTime.UtcNow
                });
            }
#pragma warning restore CA1031
        })
        .WithName("TestErpConnection")
        .WithSummary("Belirli ERP sağlayıcısına bağlantı testi");

        // POST /api/v1/erp/sync/stock — trigger manual ERP stock sync
        group.MapPost("/sync/stock", async (
            IErpAdapterFactory factory,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            logger.LogInformation("[ErpEndpoints] Manual ERP stock sync triggered");

            var results = new List<object>();

            foreach (var provider in factory.SupportedProviders)
            {
#pragma warning disable CA1031 // Intentional: per-adapter isolation
                try
                {
                    var adapter = factory.GetAdapter(provider);
                    if (adapter is IErpStockCapable stockCapable)
                    {
                        var items = await stockCapable.GetStockLevelsAsync(ct);
                        results.Add(new
                        {
                            provider = provider.ToString(),
                            success = true,
                            itemCount = items.Count
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            provider = provider.ToString(),
                            success = false,
                            itemCount = 0,
                            reason = "Not stock-capable"
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ERP stock sync failed for {Provider}", provider);
                    results.Add(new
                    {
                        provider = provider.ToString(),
                        success = false,
                        itemCount = 0,
                        reason = "Stok senkronizasyonu basarisiz — detaylar server log'unda."
                    });
                }
#pragma warning restore CA1031
            }

            return Results.Ok(ApiResponse<object>.Ok(new { results, triggeredAt = DateTime.UtcNow }));
        })
        .WithName("SyncErpStock")
        .WithSummary("Manuel ERP stok senkronizasyonu tetikle");

        // POST /api/v1/erp/sync/accounts — trigger manual ERP account sync
        group.MapPost("/sync/accounts", async (
            IErpAdapterFactory factory,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.ErpEndpoints");
            logger.LogInformation("[ErpEndpoints] Manual ERP account sync triggered");

            var results = new List<object>();

            foreach (var provider in factory.SupportedProviders)
            {
#pragma warning disable CA1031 // Intentional: per-adapter isolation
                try
                {
                    var adapter = factory.GetAdapter(provider);
                    var accounts = await adapter.GetAccountBalancesAsync(ct);
                    results.Add(new
                    {
                        provider = provider.ToString(),
                        success = true,
                        accountCount = accounts.Count,
                        totalBalance = accounts.Sum(a => a.Balance)
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ERP account sync failed for {Provider}", provider);
                    results.Add(new
                    {
                        provider = provider.ToString(),
                        success = false,
                        accountCount = 0,
                        reason = "Hesap senkronizasyonu basarisiz — detaylar server log'unda."
                    });
                }
#pragma warning restore CA1031
            }

            return Results.Ok(ApiResponse<object>.Ok(new { results, triggeredAt = DateTime.UtcNow }));
        })
        .WithName("SyncErpAccounts")
        .WithSummary("Manuel ERP cari hesap senkronizasyonu tetikle");

        // GET /api/v1/erp/sync/history — ERP senkronizasyon geçmişi
        group.MapGet("/sync/history", async (
            Guid tenantId,
            int? page,
            int? pageSize,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetErpSyncHistoryQuery(tenantId, page ?? 1, pageSize ?? 20), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpSyncHistory")
        .WithSummary("ERP senkronizasyon geçmişi")
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
        .CacheOutput("Dashboard30s");

        // GET /api/v1/erp/sync/logs — ERP sync log listesi
        group.MapGet("/sync/logs", async (
            Guid tenantId, int? page, int? pageSize,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetErpSyncLogsQuery(tenantId, page ?? 1, pageSize ?? 20), ct);
            return Results.Ok(result);
        })
        .WithName("GetErpSyncLogs")
        .WithSummary("ERP senkronizasyon log listesi")
        .CacheOutput("Report120s");
    }

    // ── Request DTOs ──────────────────────────────────────────────────

    private record ErpTestConnectionRequest(string Provider);
}
