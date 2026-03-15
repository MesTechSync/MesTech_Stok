using MediatR;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;

/// <summary>
/// Placeholder sync handler — tedarikçi sync zamanını günceller.
/// Gerçek API entegrasyonu sonraki dalga'da implemente edilecek.
/// </summary>
public class SyncDropshipProductsHandler : IRequestHandler<SyncDropshipProductsCommand, int>
{
    private readonly IDropshipSupplierRepository _supplierRepo;
    private readonly IDropshipProductRepository _productRepo;
    private readonly IUnitOfWork _uow;

    public SyncDropshipProductsHandler(
        IDropshipSupplierRepository supplierRepo,
        IDropshipProductRepository productRepo,
        IUnitOfWork uow)
    {
        _supplierRepo = supplierRepo;
        _productRepo = productRepo;
        _uow = uow;
    }

    public async Task<int> Handle(SyncDropshipProductsCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepo.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier not found: {request.SupplierId}");

        if (supplier.TenantId != request.TenantId)
            throw new InvalidOperationException("Supplier does not belong to the specified tenant.");

        // Placeholder: mark sync timestamp on supplier
        supplier.RecordSync();
        await _supplierRepo.UpdateAsync(supplier, cancellationToken);

        // Placeholder: in a real implementation, this would fetch products from the supplier API
        // and upsert them. For now, we return 0 as no products were actually synced from external source.
        await _uow.SaveChangesAsync(cancellationToken);
        return 0;
    }
}
