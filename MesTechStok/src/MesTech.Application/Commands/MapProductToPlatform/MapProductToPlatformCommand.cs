using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.MapProductToPlatform;

public record MapProductToPlatformCommand(
    Guid ProductId,
    PlatformType Platform,
    string PlatformCategoryId
) : IRequest;
