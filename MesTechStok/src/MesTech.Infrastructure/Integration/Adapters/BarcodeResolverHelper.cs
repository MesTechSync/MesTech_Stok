using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Integration.Adapters;

/// <summary>
/// Shared barcode/SKU/externalId resolver — tüm adapter'lar PushStock/PushPrice'ta
/// Guid yerine platform-specific identifier kullanmak için bu helper'ı çağırır.
///
/// Resolution chain:
/// 1. ProductPlatformMapping.ExternalProductId (platform-specific ID)
/// 2. Product.Barcode (EAN13/UPC)
/// 3. Product.SKU
/// 4. null (caller ABORT etmeli)
/// </summary>
internal static class BarcodeResolverHelper
{
    /// <summary>
    /// Resolves platform-specific product identifier from ProductPlatformMapping or Product entity.
    /// Returns null if no mapping found — caller should abort the API call.
    /// </summary>
    public static async Task<string?> ResolveAsync(
        IServiceScopeFactory? scopeFactory,
        Guid productId,
        PlatformType platformType,
        ILogger logger,
        CancellationToken ct)
    {
        if (scopeFactory is null)
        {
            logger.LogWarning("{Platform} barcode resolver: IServiceScopeFactory not available — " +
                "cannot resolve, falling back to Guid (will likely fail on platform API)",
                platformType);
            return productId.ToString();
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var mappingRepo = scope.ServiceProvider.GetService<IProductPlatformMappingRepository>();
            if (mappingRepo is not null)
            {
                var mappings = await mappingRepo.GetByProductIdAsync(productId, ct).ConfigureAwait(false);
                var mapping = mappings.FirstOrDefault(m =>
                    m.PlatformType == platformType && m.IsEnabled);

                if (mapping is not null && !string.IsNullOrEmpty(mapping.ExternalProductId))
                    return mapping.ExternalProductId;
            }

            // Fallback: Product.Barcode or SKU
            var productRepo = scope.ServiceProvider.GetService<IProductRepository>();
            if (productRepo is not null)
            {
                var product = await productRepo.GetByIdAsync(productId, ct).ConfigureAwait(false);
                if (product is not null)
                {
                    var barcode = product.Barcode ?? product.SKU;
                    if (!string.IsNullOrEmpty(barcode))
                        return barcode;
                }
            }

            logger.LogWarning("{Platform}: no barcode/externalId found for ProductId={ProductId}",
                platformType, productId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "{Platform}: barcode resolution failed for ProductId={ProductId}",
                platformType, productId);
            return null;
        }
    }
}
