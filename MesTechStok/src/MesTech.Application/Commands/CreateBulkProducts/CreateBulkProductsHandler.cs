using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateBulkProducts;

public sealed class CreateBulkProductsHandler : IRequestHandler<CreateBulkProductsCommand, CreateBulkProductsResult>
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public CreateBulkProductsHandler(
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<CreateBulkProductsResult> Handle(
        CreateBulkProductsCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var count = Math.Clamp(request.Count, 1, 500);
        var created = 0;

        for (var i = 0; i < count; i++)
        {
            var sku = $"BULK-{tenantId.ToString()[..8]}-{DateTime.UtcNow.Ticks % 1_000_000}-{i:D4}";
            var product = new Product
            {
                TenantId = tenantId,
                Name = $"Toplu Ürün #{i + 1}",
                SKU = sku,
                Barcode = $"869{DateTime.UtcNow.Ticks % 10_000_000_000:D10}",
                PurchasePrice = 10m + i,
                SalePrice = 20m + i * 2,
                Stock = 100,
                IsActive = true
            };
            product.MarkAsCreated();

            await _productRepo.AddAsync(product).ConfigureAwait(false);
            created++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateBulkProductsResult
        {
            IsSuccess = true,
            CreatedCount = created,
            Message = $"{created} ürün başarıyla oluşturuldu."
        };
    }
}
