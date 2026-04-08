using Mapster;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetBarcodeScanLogs;

public sealed class GetBarcodeScanLogsHandler : IRequestHandler<GetBarcodeScanLogsQuery, GetBarcodeScanLogsResult>
{
    private readonly IBarcodeScanLogRepository _repository;

    public GetBarcodeScanLogsHandler(IBarcodeScanLogRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<GetBarcodeScanLogsResult> Handle(
        GetBarcodeScanLogsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var items = await _repository.GetPagedAsync(
            request.Page, request.PageSize,
            request.BarcodeFilter, request.SourceFilter,
            request.IsValidFilter, request.From, request.To,
            cancellationToken);

        var count = await _repository.GetCountAsync(
            request.BarcodeFilter, request.SourceFilter,
            request.IsValidFilter, request.From, request.To,
            cancellationToken);

        return new GetBarcodeScanLogsResult
        {
            Items = items.Adapt<List<BarcodeScanLogDto>>().AsReadOnly(),
            TotalCount = count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
