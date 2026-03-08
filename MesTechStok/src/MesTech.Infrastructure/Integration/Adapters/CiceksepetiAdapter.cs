using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Ciceksepeti platform adaptoru — iskelet.
/// Dalga 3'te implement edilecek.
/// </summary>
public class CiceksepetiAdapter : IIntegratorAdapter, IWebhookCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CiceksepetiAdapter> _logger;
    private bool _isConfigured;

    public CiceksepetiAdapter(HttpClient httpClient, ILogger<CiceksepetiAdapter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string PlatformCode => nameof(PlatformType.Ciceksepeti);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti adapter FAZ 2'de implement edilecek");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti adapter FAZ 2'de implement edilecek");

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti adapter FAZ 2'de implement edilecek");

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti adapter FAZ 2'de implement edilecek");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("CiceksepetiAdapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "Ciceksepeti adapter henuz implement edilmedi (FAZ 2)",
            ResponseTime = TimeSpan.Zero
        });
    }

    public Task<bool> RegisterWebhookAsync(string callbackUrl, CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti webhook FAZ 2'de implement edilecek");

    public Task<bool> UnregisterWebhookAsync(CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti webhook FAZ 2'de implement edilecek");

    public Task ProcessWebhookPayloadAsync(string payload, CancellationToken ct = default)
        => throw new NotImplementedException("Ciceksepeti webhook FAZ 2'de implement edilecek");
}
