using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;

public record GetSettlementBatchesQuery(Guid TenantId, DateTime? From = null, DateTime? To = null, string? Platform = null)
    : IRequest<IReadOnlyList<SettlementBatchDto>>;
