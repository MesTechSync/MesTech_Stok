using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.SyncPlatform;

public record SyncPlatformCommand(
    string PlatformCode,
    SyncDirection Direction,
    DateTime? Since = null
) : IRequest<SyncResultDto>;
