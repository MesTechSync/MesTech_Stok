using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;

public record GetPlatformCommissionRatesQuery(
    Guid TenantId,
    PlatformType? Platform = null,
    bool? IsActive = true
) : IRequest<IReadOnlyList<PlatformCommissionRateDto>>;
