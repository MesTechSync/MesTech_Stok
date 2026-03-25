using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Queries.GetBackupHistory;

public sealed class GetBackupHistoryHandler : IRequestHandler<GetBackupHistoryQuery, IReadOnlyList<BackupEntryDto>>
{
    private readonly ILogger<GetBackupHistoryHandler> _logger;
    public GetBackupHistoryHandler(ILogger<GetBackupHistoryHandler> logger) => _logger = logger;

    public Task<IReadOnlyList<BackupEntryDto>> Handle(GetBackupHistoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Backup history sorgulanıyor — TenantId={TenantId}", request.TenantId);
        return Task.FromResult<IReadOnlyList<BackupEntryDto>>(Array.Empty<BackupEntryDto>());
    }
}
