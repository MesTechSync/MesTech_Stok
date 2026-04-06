using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Commands.CreateManualBackup;

public sealed class CreateManualBackupHandler : IRequestHandler<CreateManualBackupCommand, CreateManualBackupResult>
{
    private readonly IBackupEntryRepository _backupRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateManualBackupHandler> _logger;

    public CreateManualBackupHandler(
        IBackupEntryRepository backupRepo,
        IUnitOfWork uow,
        ILogger<CreateManualBackupHandler> logger)
    {
        _backupRepo = backupRepo ?? throw new ArgumentNullException(nameof(backupRepo));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CreateManualBackupResult> Handle(
        CreateManualBackupCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fileName = $"backup_{request.TenantId.ToString()[..8]}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";

        var entry = BackupEntry.Create(request.TenantId, fileName);

        _logger.LogInformation(
            "Manual backup initiated — TenantId={TenantId}, FileName={FileName}",
            request.TenantId, fileName);

        await _backupRepo.AddAsync(entry, cancellationToken).ConfigureAwait(false);

        // Mark as completed (actual backup is handled by infrastructure/scheduler)
        entry.MarkCompleted(0);

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateManualBackupResult
        {
            IsSuccess = true,
            BackupId = entry.Id,
            FileName = fileName
        };
    }
}
