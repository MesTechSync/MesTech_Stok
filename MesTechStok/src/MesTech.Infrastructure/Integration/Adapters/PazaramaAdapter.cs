using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Auth;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Pazarama platform adaptoru — iskelet. OAuth 2.0 Client Credentials auth.
/// Dalga 4'te implement edilecek (DEV 3).
/// </summary>
public class PazaramaAdapter : IIntegratorAdapter, IOrderCapableAdapter, IShipmentCapableAdapter
{
    private readonly HttpClient _httpClient;
    private readonly OAuth2AuthProvider _authProvider;
    private readonly ILogger<PazaramaAdapter> _logger;
    private bool _isConfigured;

    public PazaramaAdapter(
        HttpClient httpClient,
        OAuth2AuthProvider authProvider,
        ILogger<PazaramaAdapter> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string PlatformCode => nameof(PlatformType.Pazarama);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    // --- IIntegratorAdapter ---

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("PazaramaAdapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "Pazarama adapter henuz implement edilmedi",
            ResponseTime = TimeSpan.Zero
        });
    }

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());

    // --- IOrderCapableAdapter ---

    public Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");

    public Task<bool> UpdateOrderStatusAsync(string packageId, string status, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");

    // --- IShipmentCapableAdapter ---

    public Task<bool> SendShipmentAsync(string platformOrderId, string trackingNumber,
        CargoProvider provider, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("Pazarama adapter henuz implement edilmedi");
}
