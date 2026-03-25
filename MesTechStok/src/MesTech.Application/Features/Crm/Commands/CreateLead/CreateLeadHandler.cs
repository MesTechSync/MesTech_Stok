using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.CreateLead;

public sealed class CreateLeadHandler : IRequestHandler<CreateLeadCommand, Guid>
{
    private readonly ICrmLeadRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateLeadHandler(ICrmLeadRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateLeadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var lead = Lead.Create(
            request.TenantId,
            request.FullName,
            request.Source,
            request.Email,
            request.Phone,
            request.Company,
            request.StoreId,
            request.AssignedToUserId);

        await _repository.AddAsync(lead, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return lead.Id;
    }
}
