using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;

public class RecordTaxWithholdingHandler : IRequestHandler<RecordTaxWithholdingCommand, Guid>
{
    private readonly ITaxWithholdingRepository _repository;
    private readonly IUnitOfWork _uow;

    public RecordTaxWithholdingHandler(ITaxWithholdingRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(RecordTaxWithholdingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var withholding = TaxWithholding.Create(
            request.TenantId, request.TaxExclusiveAmount, request.Rate, request.TaxType, request.InvoiceId);

        await _repository.AddAsync(withholding, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return withholding.Id;
    }
}
