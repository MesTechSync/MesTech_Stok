using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;

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

            return Results.Ok(new { providers, count = providers.Count });
        });

        // GET /api/v1/erp/status — ping all registered ERP adapters
        group.MapGet("/status", async (IErpAdapterFactory factory, CancellationToken ct) =>
        {
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
                catch
                {
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

            return Results.Ok(new { statuses, timestamp = DateTime.UtcNow });
        });

        // POST /api/v1/erp/test-connection — test connection to a specific ERP provider
        group.MapPost("/test-connection", async (
            ErpTestConnectionRequest request,
            IErpAdapterFactory factory,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<ErpProvider>(request.Provider, ignoreCase: true, out var provider)
                || provider == ErpProvider.None)
            {
                return Results.BadRequest(new { error = $"Invalid provider: '{request.Provider}'" });
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
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Ok(new
                {
                    provider = provider.ToString(),
                    connected = false,
                    error = ex.Message,
                    testedAt = DateTime.UtcNow
                });
            }
#pragma warning restore CA1031
        });

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
                    results.Add(new
                    {
                        provider = provider.ToString(),
                        success = false,
                        itemCount = 0,
                        reason = ex.Message
                    });
                }
#pragma warning restore CA1031
            }

            return Results.Ok(new { results, triggeredAt = DateTime.UtcNow });
        });

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
                    results.Add(new
                    {
                        provider = provider.ToString(),
                        success = false,
                        accountCount = 0,
                        reason = ex.Message
                    });
                }
#pragma warning restore CA1031
            }

            return Results.Ok(new { results, triggeredAt = DateTime.UtcNow });
        });

        // GET /api/v1/erp/sync/history — DEV1-DEPENDENCY: GetErpSyncHistoryQuery handler gerekli
        group.MapGet("/sync/history", (int? limit) =>
        {
            var maxItems = limit ?? 50;
            return Results.Ok(new
            {
                history = Array.Empty<object>(),
                message = "DEV1-DEPENDENCY: GetErpSyncHistoryQuery handler not yet created",
                limit = maxItems
            });
        })
        .WithName("GetErpSyncHistory")
        .WithSummary("ERP senkronizasyon geçmişi (DEV1-DEPENDENCY)");
    }

    // ── Request DTOs ──────────────────────────────────────────────────

    private record ErpTestConnectionRequest(string Provider);
}
