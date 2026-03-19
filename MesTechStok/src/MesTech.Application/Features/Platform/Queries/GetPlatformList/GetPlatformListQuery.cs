using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformList;

public record GetPlatformListQuery(Guid TenantId) : IRequest<List<PlatformCardDto>>;
