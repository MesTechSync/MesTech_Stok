using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetSettlementBatches;

public sealed class GetSettlementBatchesHandler : IRequestHandler<GetSettlementBatchesQuery, IReadOnlyList<SettlementBatchDto>>
{
    private readonly ISettlementBatchRepository _repository;

    public GetSettlementBatchesHandler(ISettlementBatchRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<SettlementBatchDto>> Handle(GetSettlementBatchesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var from = request.From ?? DateTime.UtcNow.AddMonths(-3);
        var to = request.To ?? DateTime.UtcNow;

        var batches = !string.IsNullOrWhiteSpace(request.Platform)
            ? await _repository.GetByPlatformAsync(request.TenantId, request.Platform, cancellationToken)
            : await _repository.GetByDateRangeAsync(request.TenantId, from, to, cancellationToken);

        return batches.Select(b => new SettlementBatchDto
        {
            Id = b.Id,
            Platform = b.Platform,
            PeriodStart = b.PeriodStart,
            PeriodEnd = b.PeriodEnd,
            TotalGross = b.TotalGross,
            TotalCommission = b.TotalCommission,
            TotalNet = b.TotalNet,
            Status = b.Status.ToString(),
            ImportedAt = b.ImportedAt,
            LineCount = b.Lines.Count
        }).ToList().AsReadOnly();
    }
}
