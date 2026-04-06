using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.ImportSettlement;

public sealed class ImportSettlementHandler : IRequestHandler<ImportSettlementCommand, Guid>
{
    private readonly ISettlementBatchRepository _repository;
    private readonly IUnitOfWork _uow;

    public ImportSettlementHandler(ISettlementBatchRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(ImportSettlementCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var batch = SettlementBatch.Create(
            request.TenantId, request.Platform, request.PeriodStart, request.PeriodEnd,
            request.TotalGross, request.TotalCommission, request.TotalNet);

        foreach (var line in request.Lines)
        {
            var settlementLine = SettlementLine.Create(
                request.TenantId, batch.Id, line.OrderId,
                line.GrossAmount, line.CommissionAmount, line.ServiceFee,
                line.CargoDeduction, line.RefundDeduction, line.NetAmount,
                line.VatAmount);
            batch.AddLine(settlementLine);
        }

        await _repository.AddAsync(batch, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return batch.Id;
    }
}
