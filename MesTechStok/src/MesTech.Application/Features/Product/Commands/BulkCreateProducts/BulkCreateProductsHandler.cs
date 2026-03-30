using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Commands.BulkCreateProducts;

/// <summary>
/// Toplu urun olusturma handler'i.
/// SKU bazinda duplikat kontrolu yapar, batch halinde olusturur.
/// </summary>
public sealed class BulkCreateProductsHandler : IRequestHandler<BulkCreateProductsCommand, BulkCreateProductsResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<BulkCreateProductsHandler> _logger;

    public BulkCreateProductsHandler(
        IProductRepository productRepository,
        IUnitOfWork uow,
        ILogger<BulkCreateProductsHandler> logger)
    {
        _productRepository = productRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task<BulkCreateProductsResult> Handle(
        BulkCreateProductsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<string>();
        var successCount = 0;

        // Fetch existing SKUs in batch to detect duplicates
        var incomingSkus = request.Products
            .Select(p => p.SKU)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingProducts = await _productRepository.GetBySKUsAsync(incomingSkus, cancellationToken);
        var existingSkuSet = new HashSet<string>(
            existingProducts.Select(p => p.SKU),
            StringComparer.OrdinalIgnoreCase);

        // Track SKUs within this batch to detect intra-batch duplicates
        var batchSkuSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < request.Products.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var input = request.Products[i];
            var rowIndex = i + 1;

            // Validate required fields
            if (string.IsNullOrWhiteSpace(input.Name))
            {
                errors.Add($"Satir {rowIndex}: Urun adi bos olamaz.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.SKU))
            {
                errors.Add($"Satir {rowIndex}: SKU bos olamaz.");
                continue;
            }

            // Check duplicate in database
            if (existingSkuSet.Contains(input.SKU))
            {
                errors.Add($"Satir {rowIndex}: SKU '{input.SKU}' zaten mevcut.");
                continue;
            }

            // Check duplicate within batch
            if (!batchSkuSet.Add(input.SKU))
            {
                errors.Add($"Satir {rowIndex}: SKU '{input.SKU}' dosya icinde tekrar ediyor.");
                continue;
            }

            try
            {
                var product = new Domain.Entities.Product
                {
                    TenantId = request.TenantId,
                    Name = input.Name,
                    SKU = input.SKU,
                    SalePrice = input.Price,
                    Barcode = input.Barcode,
                    Description = input.Description
                };

                if (input.CategoryId.HasValue && input.CategoryId.Value != Guid.Empty)
                    product.CategoryId = input.CategoryId.Value;

                if (input.Quantity != 0)
                    product.AdjustStock(input.Quantity, Domain.Enums.StockMovementType.Purchase, "Toplu import");

                await _productRepository.AddAsync(product);
                successCount++;
            }
#pragma warning disable CA1031 // Catch general exception — batch processing must not abort on single item failure
            catch (Exception ex)
#pragma warning restore CA1031
            {
                errors.Add($"Satir {rowIndex}: {ex.Message}");
                _logger.LogWarning(ex, "Toplu urun olusturma hatasi — Satir: {Row}, SKU: {SKU}", rowIndex, input.SKU);
            }
        }

        if (successCount > 0)
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Toplu urun olusturma tamamlandi: {Success}/{Total} basarili, {Fail} hatali",
            successCount, request.Products.Count, errors.Count);

        return new BulkCreateProductsResult
        {
            TotalReceived = request.Products.Count,
            SuccessCount = successCount,
            FailCount = errors.Count,
            Errors = errors
        };
    }
}
