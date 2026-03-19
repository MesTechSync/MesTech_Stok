using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.CategoryMapping.Commands.MapCategory;

public record MapCategoryCommand(
    Guid TenantId,
    Guid InternalCategoryId,
    PlatformType Platform,
    string PlatformCategoryId,
    string PlatformCategoryName
) : IRequest<Guid>;
