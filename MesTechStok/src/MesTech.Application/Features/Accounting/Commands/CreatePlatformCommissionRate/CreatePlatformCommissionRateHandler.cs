using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;

public class CreatePlatformCommissionRateHandler : IRequestHandler<CreatePlatformCommissionRateCommand, Guid>
{
    private readonly IPlatformCommissionRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreatePlatformCommissionRateHandler(IPlatformCommissionRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreatePlatformCommissionRateCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var commission = new PlatformCommission
        {
            TenantId = request.TenantId,
            Platform = request.Platform,
            Type = request.Type,
            CategoryName = request.CategoryName,
            PlatformCategoryId = request.PlatformCategoryId,
            Rate = request.Rate,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            Currency = request.Currency,
            EffectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow,
            EffectiveTo = request.EffectiveTo,
            IsActive = true,
            Notes = request.Notes
        };

        await _repository.AddAsync(commission, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return commission.Id;
    }
}
