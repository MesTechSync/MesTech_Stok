using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Platform.Commands.CreatePlatformAttributeValueMapping;

public sealed class CreatePlatformAttributeValueMappingHandler : IRequestHandler<CreatePlatformAttributeValueMappingCommand, Guid>
{
    private readonly IPlatformAttributeValueMappingRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreatePlatformAttributeValueMappingHandler(IPlatformAttributeValueMappingRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreatePlatformAttributeValueMappingCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mapping = PlatformAttributeValueMapping.Create(
            request.TenantId,
            request.InternalAttributeName,
            request.InternalValue,
            request.PlatformType,
            request.PlatformAttributeId,
            request.PlatformValueId,
            request.PlatformValueName,
            request.IsSlicer,
            request.IsVarianter);

        await _repository.AddAsync(mapping, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return mapping.Id;
    }
}
