using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// N11 platform adaptoru — iskelet. SOAP wrapper gerekli.
/// FAZ 2'de implement edilecek.
/// </summary>
public class N11Adapter : IIntegratorAdapter
{
    private readonly ILogger<N11Adapter> _logger;

    public N11Adapter(ILogger<N11Adapter> logger)
    {
        _logger = logger;
    }

    public string PlatformCode => nameof(PlatformType.N11);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new NotImplementedException("N11 adapter FAZ 2'de implement edilecek — SOAP wrapper gerekli");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new NotImplementedException("N11 adapter FAZ 2'de implement edilecek — SOAP wrapper gerekli");

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => throw new NotImplementedException("N11 adapter FAZ 2'de implement edilecek");

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => throw new NotImplementedException("N11 adapter FAZ 2'de implement edilecek");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("N11Adapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "N11 adapter henuz implement edilmedi (FAZ 2)",
            ResponseTime = TimeSpan.Zero
        });
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
}
