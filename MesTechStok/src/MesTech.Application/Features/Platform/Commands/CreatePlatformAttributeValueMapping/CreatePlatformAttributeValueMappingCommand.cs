using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Platform.Commands.CreatePlatformAttributeValueMapping;

public record CreatePlatformAttributeValueMappingCommand(
    Guid TenantId,
    string InternalAttributeName,
    string InternalValue,
    PlatformType PlatformType,
    int? PlatformAttributeId = null,
    int? PlatformValueId = null,
    string? PlatformValueName = null,
    bool IsSlicer = false,
    bool IsVarianter = false
) : IRequest<Guid>;
