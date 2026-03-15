using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Etsy platform adaptoru — STUB (Dalga 10 C-03).
/// Tam implementasyon Sprint D'de planlanmaktadir.
/// Auth: OAuth 2.0 (PKCE) — implementasyon bekliyor.
/// API: https://openapi.etsy.com/v3/
/// </summary>
public class EtsyAdapter : IIntegratorAdapter
{
    private readonly ILogger<EtsyAdapter> _logger;

    public EtsyAdapter(ILogger<EtsyAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — Identity
    // ─────────────────────────────────────────────

    public string PlatformCode => "Etsy";
    public bool SupportsStockUpdate => false;
    public bool SupportsPriceUpdate => false;
    public bool SupportsShipment => false;

    // ─────────────────────────────────────────────
    // IIntegratorAdapter — Stubs
    // ─────────────────────────────────────────────

    public Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogInformation("EtsyAdapter.TestConnectionAsync — stub (Sprint D'de impl)");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            IsSuccess = false,
            ErrorMessage = "Etsy adaptoru henuz implemente edilmedi (Sprint D'de impl)"
        });
    }

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("EtsyAdapter.PullProductsAsync — stub (Sprint D'de impl)");
        return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("EtsyAdapter.PushProductAsync — stub (Sprint D'de impl)");
        return Task.FromResult(false);
    }

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
    {
        _logger.LogInformation("EtsyAdapter.PushStockUpdateAsync — stub (Sprint D'de impl)");
        return Task.FromResult(false);
    }

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
    {
        _logger.LogInformation("EtsyAdapter.PushPriceUpdateAsync — stub (Sprint D'de impl)");
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
}
