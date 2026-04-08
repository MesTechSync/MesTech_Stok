using MediatR;

namespace MesTech.Application.Features.System.Commands.CreateManualBackup;

public record CreateManualBackupCommand(
    Guid TenantId,
    string? Description = null
) : IRequest<CreateManualBackupResult>;

public sealed class CreateManualBackupResult
{
    public bool IsSuccess { get; init; }
    public Guid? BackupId { get; init; }
    public string? FileName { get; init; }
    public string? ErrorMessage { get; init; }
}
