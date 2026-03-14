using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetExportHistoryQuery(
    Guid? PoolId = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ExportHistoryDto>>;

public record ExportHistoryDto(
    Guid Id,
    Guid PoolId,
    string ExportFormat,
    int ProductCount,
    DateTime ExportedAt,
    string ExportedBy,
    bool IsSuccess,
    string? ErrorMessage
);

/// <summary>
/// Export geçmişi için stub handler — IExportHistoryRepository Sprint B-06'da eklenir.
/// Şu an boş liste döner.
/// </summary>
public class GetExportHistoryQueryHandler
    : IRequestHandler<GetExportHistoryQuery, PagedResult<ExportHistoryDto>>
{
    public Task<PagedResult<ExportHistoryDto>> Handle(
        GetExportHistoryQuery req, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            PagedResult<ExportHistoryDto>.Empty(req.Page, req.PageSize));
    }
}
