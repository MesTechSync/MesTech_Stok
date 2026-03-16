using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;

public class UpdatePlatformCommissionRateHandler : IRequestHandler<UpdatePlatformCommissionRateCommand, bool>
{
    private readonly IPlatformCommissionRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdatePlatformCommissionRateHandler(IPlatformCommissionRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<bool> Handle(UpdatePlatformCommissionRateCommand request, CancellationToken cancellationToken)
    {
        var commission = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (commission == null) return false;

        if (request.Rate.HasValue)
            commission.Rate = request.Rate.Value;
        if (request.Type.HasValue)
            commission.Type = request.Type.Value;
        if (request.CategoryName != null)
            commission.CategoryName = request.CategoryName;
        if (request.PlatformCategoryId != null)
            commission.PlatformCategoryId = request.PlatformCategoryId;
        if (request.MinAmount.HasValue)
            commission.MinAmount = request.MinAmount;
        if (request.MaxAmount.HasValue)
            commission.MaxAmount = request.MaxAmount;
        if (request.Currency != null)
            commission.Currency = request.Currency;
        if (request.EffectiveFrom.HasValue)
            commission.EffectiveFrom = request.EffectiveFrom.Value;
        if (request.EffectiveTo.HasValue)
            commission.EffectiveTo = request.EffectiveTo;
        if (request.IsActive.HasValue)
            commission.IsActive = request.IsActive.Value;
        if (request.Notes != null)
            commission.Notes = request.Notes;

        commission.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(commission, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
