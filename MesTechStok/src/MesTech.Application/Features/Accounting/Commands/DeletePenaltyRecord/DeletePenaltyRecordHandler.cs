using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.DeletePenaltyRecord;

public sealed class DeletePenaltyRecordHandler : IRequestHandler<DeletePenaltyRecordCommand>
{
    private readonly IPenaltyRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeletePenaltyRecordHandler(IPenaltyRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeletePenaltyRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"PenaltyRecord {request.Id} not found.");

        record.IsDeleted = true;
        record.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
