using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;

public record CreatePlatformCommissionRateCommand(
    Guid TenantId,
    PlatformType Platform,
    decimal Rate,
    CommissionType Type = CommissionType.Percentage,
    string? CategoryName = null,
    string? PlatformCategoryId = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    string Currency = "TRY",
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null,
    string? Notes = null
) : IRequest<Guid>;
