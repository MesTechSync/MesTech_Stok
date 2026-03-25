using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;

public sealed class GetCategoryMappingsHandler
    : IRequestHandler<GetCategoryMappingsQuery, List<CategoryMappingViewDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryPlatformMappingRepository _mappingRepository;

    public GetCategoryMappingsHandler(
        ICategoryRepository categoryRepository,
        ICategoryPlatformMappingRepository mappingRepository)
    {
        _categoryRepository = categoryRepository;
        _mappingRepository = mappingRepository;
    }

    public async Task<List<CategoryMappingViewDto>> Handle(
        GetCategoryMappingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var categories = await _categoryRepository.GetAllAsync();
        var mappings = await _mappingRepository
            .GetByTenantAsync(request.TenantId, request.Platform, cancellationToken);

        var mappingLookup = mappings
            .GroupBy(m => m.CategoryId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<CategoryMappingViewDto>();

        foreach (var category in categories)
        {
            var hasMapping = mappingLookup.TryGetValue(category.Id, out var mapping);

            result.Add(new CategoryMappingViewDto
            {
                MappingId = mapping?.Id ?? Guid.Empty,
                InternalCategoryId = category.Id,
                InternalCategoryName = category.Name,
                PlatformCategoryId = mapping?.ExternalCategoryId,
                PlatformCategoryName = mapping?.ExternalCategoryName,
                IsMapped = hasMapping
            });
        }

        return result;
    }
}
