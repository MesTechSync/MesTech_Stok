using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Logging.Commands.CleanOldLogs;

public sealed class CleanOldLogsHandler : IRequestHandler<CleanOldLogsCommand, int>
{
    private readonly ILogEntryRepository _repo;
    private readonly ILogger<CleanOldLogsHandler> _logger;

    public CleanOldLogsHandler(ILogEntryRepository repo, ILogger<CleanOldLogsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<int> Handle(CleanOldLogsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cutoffDate = DateTime.UtcNow.AddDays(-request.DaysToKeep);
        var deletedCount = await _repo.DeleteOlderThanAsync(request.TenantId, cutoffDate, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Log cleanup: {DeletedCount} entries older than {CutoffDate} deleted for tenant {TenantId}",
            deletedCount, cutoffDate, request.TenantId);

        return deletedCount;
    }
}
