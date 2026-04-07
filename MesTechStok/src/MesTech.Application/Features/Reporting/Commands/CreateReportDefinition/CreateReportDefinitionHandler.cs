using MediatR;
using MesTech.Application.Interfaces.Reporting;
using MesTech.Domain.Entities.Reporting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Reporting.Commands.CreateReportDefinition;

public sealed class CreateReportDefinitionHandler : IRequestHandler<CreateReportDefinitionCommand, Guid>
{
    private readonly IReportDefinitionRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateReportDefinitionHandler(IReportDefinitionRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateReportDefinitionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = ReportDefinition.Create(
            request.TenantId,
            request.Name,
            request.Type,
            request.Frequency,
            request.RecipientEmail);

        await _repository.AddAsync(definition, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return definition.Id;
    }
}
