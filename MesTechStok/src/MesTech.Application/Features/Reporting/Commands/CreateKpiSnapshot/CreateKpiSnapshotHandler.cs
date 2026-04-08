using MediatR;
using MesTech.Application.Interfaces.Reporting;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reporting.Commands.CreateKpiSnapshot;

public sealed class CreateKpiSnapshotHandler : IRequestHandler<CreateKpiSnapshotCommand, Guid>
{
    private readonly IKpiSnapshotRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateKpiSnapshotHandler(IKpiSnapshotRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateKpiSnapshotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = KpiSnapshot.Create(
            request.TenantId, request.SnapshotDate, request.Type,
            request.Value, request.PreviousValue, request.PlatformCode);

        await _repository.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.Id;
    }
}
