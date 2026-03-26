using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.SeedDemoData;

public sealed class SeedDemoDataHandler : IRequestHandler<SeedDemoDataCommand, SeedDemoDataResult>
{
    private readonly IMediator _mediator;
    private readonly IProductRepository _productRepo;
    private readonly ITenantProvider _tenantProvider;

    public SeedDemoDataHandler(
        IMediator mediator,
        IProductRepository productRepo,
        ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _productRepo = productRepo;
        _tenantProvider = tenantProvider;
    }

    public async Task<SeedDemoDataResult> Handle(
        SeedDemoDataCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var existingCount = await _productRepo.CountByTenantAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        if (existingCount > 0)
        {
            return new SeedDemoDataResult
            {
                IsSuccess = true,
                WasSkipped = true,
                Message = $"Demo veri zaten mevcut ({existingCount} ürün). Atlandı."
            };
        }

        var bulkResult = await _mediator.Send(
            new CreateBulkProducts.CreateBulkProductsCommand(Count: 40),
            cancellationToken).ConfigureAwait(false);

        return new SeedDemoDataResult
        {
            IsSuccess = bulkResult.IsSuccess,
            WasSkipped = false,
            Message = bulkResult.Message
        };
    }
}
