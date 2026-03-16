using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;

public record UpdatePlatformCommissionRateCommand(
    Guid Id,
    decimal? Rate = null,
    CommissionType? Type = null,
    string? CategoryName = null,
    string? PlatformCategoryId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string? Currency = null,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    bool? IsActive = null,
    string? Notes = null
) : IRequest<bool>;
