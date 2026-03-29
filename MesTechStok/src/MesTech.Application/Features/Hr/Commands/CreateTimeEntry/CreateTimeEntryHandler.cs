using MediatR;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Hr.Commands.CreateTimeEntry;

public sealed class CreateTimeEntryHandler : IRequestHandler<CreateTimeEntryCommand, Guid>
{
    private readonly ITimeEntryRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTimeEntryHandler(ITimeEntryRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Guid> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = TimeEntry.Start(
            request.TenantId,
            request.WorkTaskId,
            request.UserId,
            request.Description,
            request.IsBillable,
            request.HourlyRate);

        await _repository.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
