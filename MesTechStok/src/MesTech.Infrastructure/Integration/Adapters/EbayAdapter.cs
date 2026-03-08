using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// eBay platform adaptoru — iskelet. OAuth2 + multi-currency gerekli.
/// FAZ 2'de implement edilecek.
/// </summary>
public class EbayAdapter : IIntegratorAdapter
{
    private readonly ILogger<EbayAdapter> _logger;

    public EbayAdapter(ILogger<EbayAdapter> logger)
    {
        _logger = logger;
    }

    public string PlatformCode => "eBay";
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new NotImplementedException("eBay adapter FAZ 2'de implement edilecek — OAuth2 + multi-currency");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new NotImplementedException("eBay adapter FAZ 2'de implement edilecek — OAuth2 + multi-currency");

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => throw new NotImplementedException("eBay adapter FAZ 2'de implement edilecek");

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => throw new NotImplementedException("eBay adapter FAZ 2'de implement edilecek");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("EbayAdapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "eBay adapter henuz implement edilmedi (FAZ 2)",
            ResponseTime = TimeSpan.Zero
        });
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
}
