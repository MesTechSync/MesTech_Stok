using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.MapProductToPlatform;

public sealed class MapProductToPlatformHandler : IRequestHandler<MapProductToPlatformCommand>
{
    private readonly IProductRepository _productRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public MapProductToPlatformHandler(
        IProductRepository productRepo,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider)
    {
        _productRepo = productRepo;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task Handle(MapProductToPlatformCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepo.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product is null)
            throw new KeyNotFoundException($"Product '{request.ProductId}' bulunamadı.");

        var mapping = new ProductPlatformMapping
        {
            TenantId = _tenantProvider.GetCurrentTenantId(),
            ProductId = request.ProductId,
            PlatformType = request.Platform,
            ExternalCategoryId = request.PlatformCategoryId,
            SyncStatus = SyncStatus.NotSynced,
            IsEnabled = true
        };

        await _productRepo.AddPlatformMappingAsync(mapping, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
