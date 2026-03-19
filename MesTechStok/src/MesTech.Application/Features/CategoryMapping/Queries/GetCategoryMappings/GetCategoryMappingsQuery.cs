using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;

public record GetCategoryMappingsQuery(
    Guid TenantId,
    PlatformType? Platform = null
) : IRequest<List<CategoryMappingViewDto>>;
