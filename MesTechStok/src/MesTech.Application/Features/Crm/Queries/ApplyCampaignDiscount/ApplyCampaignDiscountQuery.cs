using MediatR;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;

public record ApplyCampaignDiscountQuery(
    Guid ProductId,
    decimal Price
) : IRequest<CampaignDiscountResultDto>;
