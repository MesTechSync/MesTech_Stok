using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.CategoryMapping.Commands.MapCategory;

public sealed class MapCategoryHandler : IRequestHandler<MapCategoryCommand, Guid>
{
    private readonly ICategoryPlatformMappingRepository _mappingRepository;
    private readonly IUnitOfWork _uow;

    public MapCategoryHandler(
        ICategoryPlatformMappingRepository mappingRepository,
        IUnitOfWork uow)
    {
        _mappingRepository = mappingRepository;
        _uow = uow;
    }

    public async Task<Guid> Handle(
        MapCategoryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Upsert: check if mapping already exists
        var existing = await _mappingRepository.GetByCategoryAndPlatformAsync(
            request.TenantId,
            request.InternalCategoryId,
            request.Platform,
            cancellationToken);

        if (existing is not null)
        {
            existing.ExternalCategoryId = request.PlatformCategoryId;
            existing.ExternalCategoryName = request.PlatformCategoryName;
            existing.LastSyncDate = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName;

            await _mappingRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return existing.Id;
        }

        var mapping = new CategoryPlatformMapping
        {
            TenantId = request.TenantId,
            CategoryId = request.InternalCategoryId,
            PlatformType = request.Platform,
            ExternalCategoryId = request.PlatformCategoryId,
            ExternalCategoryName = request.PlatformCategoryName,
            LastSyncDate = DateTime.UtcNow,
            CreatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName,
            UpdatedBy = MesTech.Domain.Constants.DomainConstants.SystemUserName
        };

        await _mappingRepository.AddAsync(mapping, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return mapping.Id;
    }
}
