using System.Diagnostics;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// PTT AVM platform adaptoru — iskelet. Kargo entegrasyon gerekli.
/// FAZ 2'de implement edilecek.
/// </summary>
public class PttAvmAdapter : IIntegratorAdapter
{
    private readonly ILogger<PttAvmAdapter> _logger;

    public PttAvmAdapter(ILogger<PttAvmAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string PlatformCode => nameof(PlatformType.PttAVM);
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("PTT AVM adapter FAZ 2'de implement edilecek — kargo entegrasyon");

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
        => throw new PlatformNotSupportedException("PTT AVM adapter FAZ 2'de implement edilecek");

    public Task<bool> PushStockUpdateAsync(int productId, int newStock, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("PTT AVM adapter FAZ 2'de implement edilecek");

    public Task<bool> PushPriceUpdateAsync(int productId, decimal newPrice, CancellationToken ct = default)
        => throw new PlatformNotSupportedException("PTT AVM adapter FAZ 2'de implement edilecek");

    public Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default)
    {
        _logger.LogWarning("PttAvmAdapter.TestConnectionAsync — henuz implement edilmedi");
        return Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            ErrorMessage = "PTT AVM adapter henuz implement edilmedi (FAZ 2)",
            ResponseTime = TimeSpan.Zero
        });
    }
}
