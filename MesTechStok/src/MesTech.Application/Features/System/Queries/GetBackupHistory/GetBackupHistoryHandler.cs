using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Queries.GetBackupHistory;

public sealed class GetBackupHistoryHandler : IRequestHandler<GetBackupHistoryQuery, IReadOnlyList<BackupEntryDto>>
{
    private readonly IBackupEntryRepository _repo;
    private readonly ILogger<GetBackupHistoryHandler> _logger;

    public GetBackupHistoryHandler(IBackupEntryRepository repo, ILogger<GetBackupHistoryHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BackupEntryDto>> Handle(GetBackupHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Backup history sorgulanıyor — TenantId={TenantId}", request.TenantId);

        var entries = await _repo.GetByTenantAsync(request.TenantId, request.Limit, cancellationToken).ConfigureAwait(false);

        return entries
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new BackupEntryDto(e.Id, e.FileName, e.SizeBytes, e.Status, e.CreatedAt, e.ErrorMessage))
            .ToList()
            .AsReadOnly();
    }
}
