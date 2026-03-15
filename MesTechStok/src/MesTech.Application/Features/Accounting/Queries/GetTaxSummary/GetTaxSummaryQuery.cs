using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxSummary;

public record GetTaxSummaryQuery(Guid TenantId, string Period)
    : IRequest<TaxSummaryDto>;
