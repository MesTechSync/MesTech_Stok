using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Logging.Queries.GetLogs;

public class GetLogsHandler : IRequestHandler<GetLogsQuery, IReadOnlyList<LogEntry>>
{
    private readonly ILogEntryRepository _repo;

    public GetLogsHandler(ILogEntryRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<LogEntry>> Handle(GetLogsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _repo.GetPagedAsync(
            request.TenantId,
            request.Page,
            request.PageSize,
            category: request.Category,
            userId: request.UserId,
            productName: request.ProductName,
            barcode: request.Barcode,
            from: request.StartDate,
            to: request.EndDate,
            ct: cancellationToken);
    }
}
