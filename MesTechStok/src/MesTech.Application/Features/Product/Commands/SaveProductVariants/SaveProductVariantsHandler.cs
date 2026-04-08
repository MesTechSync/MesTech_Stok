using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Commands.SaveProductVariants;

/// <summary>
/// Urun varyantlarini toplu kaydeder — mevcut varyantlari SKU bazli gunceller, yenileri ekler.
/// Avalonia ProductVariantMatrixViewModel tarafindan cagirilir.
/// </summary>
public sealed class SaveProductVariantsHandler
    : IRequestHandler<SaveProductVariantsCommand, SaveProductVariantsResult>
{
    private readonly IProductVariantRepository _variantRepo;
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SaveProductVariantsHandler> _logger;

    public SaveProductVariantsHandler(
        IProductVariantRepository variantRepo,
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ILogger<SaveProductVariantsHandler> logger)
    {
        _variantRepo = variantRepo;
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SaveProductVariantsResult> Handle(
        SaveProductVariantsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

#pragma warning disable CA1031 // Catch general exception — return structured error
        try
        {
            var product = await _productRepo.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
            if (product is null)
                return SaveProductVariantsResult.Failure($"Urun bulunamadi: {request.ProductId}");

            var existingVariants = await _variantRepo.GetByProductIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
            var existingBySku = existingVariants.ToDictionary(v => v.SKU, StringComparer.OrdinalIgnoreCase);

            var savedCount = 0;

            foreach (var input in request.Variants)
            {
                if (existingBySku.TryGetValue(input.SKU, out var existing))
                {
                    // Update existing variant
                    existing.Color = input.Color;
                    existing.Size = input.Size;
                    existing.Price = input.Price;
                    existing.Stock = input.Stock;
                    existing.VariantBarcode = input.Barcode;
                    existing.IsActive = input.IsActive;
                    await _variantRepo.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Create new variant
                    var variant = ProductVariant.Create(request.ProductId, input.SKU, input.Stock, input.Price);
                    variant.TenantId = request.TenantId;
                    variant.Color = input.Color;
                    variant.Size = input.Size;
                    variant.VariantBarcode = input.Barcode;
                    variant.IsActive = input.IsActive;
                    await _variantRepo.AddAsync(variant, cancellationToken).ConfigureAwait(false);
                }

                savedCount++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Urun varyantlari kaydedildi: ProductId={ProductId}, Saved={Count}",
                request.ProductId, savedCount);

            return SaveProductVariantsResult.Success(savedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Urun varyantlari kaydetme hatasi: ProductId={ProductId}", request.ProductId);
            return SaveProductVariantsResult.Failure(ex.Message);
        }
#pragma warning restore CA1031
    }
}
