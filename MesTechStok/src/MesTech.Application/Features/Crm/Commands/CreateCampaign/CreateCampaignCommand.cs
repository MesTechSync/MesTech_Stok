using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Crm.Commands.CreateCampaign;

public record CreateCampaignCommand(
    Guid TenantId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    decimal DiscountPercent,
    PlatformType? PlatformType = null,
    IReadOnlyList<Guid>? ProductIds = null
) : IRequest<Guid>;
