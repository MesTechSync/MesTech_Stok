using MediatR;

namespace MesTech.Application.Queries.GetBarcodeScanLogs;

public record GetBarcodeScanLogsQuery(
    int Page = 1,
    int PageSize = 50,
    string? BarcodeFilter = null,
    string? SourceFilter = null,
    bool? IsValidFilter = null,
    DateTime? From = null,
    DateTime? To = null
) : IRequest<GetBarcodeScanLogsResult>;

public sealed class GetBarcodeScanLogsResult
{
    public IReadOnlyList<DTOs.BarcodeScanLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
