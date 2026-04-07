using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Erp.Commands.CreateErpFieldMapping;

public sealed class CreateErpFieldMappingHandler : IRequestHandler<CreateErpFieldMappingCommand, Guid>
{
    private readonly IErpFieldMappingRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateErpFieldMappingHandler(IErpFieldMappingRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateErpFieldMappingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mapping = ErpFieldMapping.Create(
            request.TenantId,
            request.ErpType,
            request.MesTechField,
            request.ErpField,
            request.IsRequired,
            request.TransformExpression);

        await _repository.AddAsync(mapping, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return mapping.Id;
    }
}
