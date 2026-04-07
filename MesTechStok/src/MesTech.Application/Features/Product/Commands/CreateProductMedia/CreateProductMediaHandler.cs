using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Product.Commands.CreateProductMedia;

public sealed class CreateProductMediaHandler : IRequestHandler<CreateProductMediaCommand, Guid>
{
    private readonly IProductMediaRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProductMediaHandler(IProductMediaRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateProductMediaCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var media = ProductMedia.Create(
            request.TenantId, request.ProductId, request.Type,
            request.Url, request.SortOrder,
            request.VariantId, request.AltText);

        await _repository.AddAsync(media, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return media.Id;
    }
}
