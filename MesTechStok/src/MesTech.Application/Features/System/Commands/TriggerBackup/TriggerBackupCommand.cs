using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.System.Commands.TriggerBackup;

/// <summary>
/// Manuel yedekleme tetikleyici — BackupEntry kaydı oluşturur.
/// G207-DEV6: Backup POST endpoint eksikliği kapatılıyor.
/// </summary>
public record TriggerBackupCommand(Guid TenantId, string? Description = null)
    : IRequest<TriggerBackupResult>;

public record TriggerBackupResult(Guid BackupId, string FileName, string Status);

public sealed class TriggerBackupHandler
    : IRequestHandler<TriggerBackupCommand, TriggerBackupResult>
{
    private readonly IBackupEntryRepository _backupRepo;
    private readonly IUnitOfWork _unitOfWork;

    public TriggerBackupHandler(IBackupEntryRepository backupRepo, IUnitOfWork unitOfWork)
    {
        _backupRepo = backupRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<TriggerBackupResult> Handle(
        TriggerBackupCommand request, CancellationToken ct)
    {
        var fileName = $"backup_{request.TenantId:N}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
        var entry = BackupEntry.Create(request.TenantId, fileName);

        await _backupRepo.AddAsync(entry, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        return new TriggerBackupResult(entry.Id, fileName, entry.Status);
    }
}
