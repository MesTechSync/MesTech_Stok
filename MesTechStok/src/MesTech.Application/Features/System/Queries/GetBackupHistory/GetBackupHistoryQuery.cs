using MediatR;

namespace MesTech.Application.Features.System.Queries.GetBackupHistory;

public record GetBackupHistoryQuery(Guid TenantId, int Limit = 20) : IRequest<IReadOnlyList<BackupEntryDto>>;

public record BackupEntryDto(Guid Id, string FileName, long SizeBytes, string Status, DateTime CreatedAt, string? ErrorMessage);
