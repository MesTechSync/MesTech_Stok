using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;

public record GetPlatformSyncStatusQuery(Guid TenantId) : IRequest<List<PlatformSyncStatusDto>>;
