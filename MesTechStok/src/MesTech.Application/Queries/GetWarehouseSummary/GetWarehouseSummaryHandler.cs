using MediatR;

namespace MesTech.Application.Queries.GetWarehouseSummary;

public class GetWarehouseSummaryHandler : IRequestHandler<GetWarehouseSummaryQuery, IReadOnlyList<WarehouseSummaryDto>>
{
    public Task<IReadOnlyList<WarehouseSummaryDto>> Handle(
        GetWarehouseSummaryQuery request, CancellationToken cancellationToken)
    {
        // Will be wired to repository when Infrastructure layer is ready
        // For now returns empty list — Avalonia ViewModel uses demo data
        IReadOnlyList<WarehouseSummaryDto> result = Array.Empty<WarehouseSummaryDto>();
        return Task.FromResult(result);
    }
}
