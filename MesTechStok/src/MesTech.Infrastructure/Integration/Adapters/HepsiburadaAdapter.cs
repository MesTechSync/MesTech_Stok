using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Hepsiburada platform adaptoru — iskelet.
/// FAZ 2'de implement edilecek.
/// </summary>
public class HepsiburadaAdapter : IIntegratorAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HepsiburadaAdapter> _logger;
    private bool _isConfigured;

    public HepsiburadaAdapter(HttpClient httpClient, ILogger<HepsiburadaAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string PlatformCode => nameof(PlatformType.Hepsiburada);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new NotImplementedException("Hepsiburada adapter FAZ 2'de implement edilecek");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Hepsiburada adapter FAZ 2'de implement edilecek");

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => throw new NotImplementedException("Hepsiburada adapter FAZ 2'de implement edilecek");

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => throw new NotImplementedException("Hepsiburada adapter FAZ 2'de implement edilecek");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("HepsiburadaAdapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "Hepsiburada adapter henuz implement edilmedi (FAZ 2)",
            ResponseTime = TimeSpan.Zero
        });
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
}
