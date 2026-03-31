using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateBaBsRecord;

/// <summary>
/// Ba/Bs kayit olusturma handler — VUK 396.
/// BaBsRecord.Create factory ile entity olusturur, domain event tetikler, veritabanina kaydeder.
/// </summary>
public sealed class CreateBaBsRecordHandler : IRequestHandler<CreateBaBsRecordCommand, Guid>
{
    private readonly IBaBsRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateBaBsRecordHandler(IBaBsRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateBaBsRecordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = BaBsRecord.Create(
            tenantId: request.TenantId,
            year: request.Year,
            month: request.Month,
            type: request.Type,
            counterpartyVkn: request.CounterpartyVkn,
            counterpartyName: request.CounterpartyName,
            totalAmount: request.TotalAmount,
            documentCount: request.DocumentCount);

        await _repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return record.Id;
    }
}
