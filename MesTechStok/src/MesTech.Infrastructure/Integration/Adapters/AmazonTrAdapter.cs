using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Amazon TR platform adaptoru — iskelet. OAuth2 auth gerekli.
/// FAZ 2'de implement edilecek.
/// </summary>
public class AmazonTrAdapter : IIntegratorAdapter
{
    private readonly ILogger<AmazonTrAdapter> _logger;

    public AmazonTrAdapter(ILogger<AmazonTrAdapter> logger)
    {
        _logger = logger;
    }

    public string PlatformCode => nameof(PlatformType.Amazon);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new NotImplementedException("Amazon TR adapter FAZ 2'de implement edilecek — OAuth2 gerekli");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Amazon TR adapter FAZ 2'de implement edilecek — OAuth2 gerekli");

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => throw new NotImplementedException("Amazon TR adapter FAZ 2'de implement edilecek");

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => throw new NotImplementedException("Amazon TR adapter FAZ 2'de implement edilecek");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("AmazonTrAdapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "Amazon TR adapter henuz implement edilmedi (FAZ 2)",
            ResponseTime = TimeSpan.Zero
        });
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
}
