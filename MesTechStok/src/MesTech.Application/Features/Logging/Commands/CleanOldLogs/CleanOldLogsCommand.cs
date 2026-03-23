using MediatR;

namespace MesTech.Application.Features.Logging.Commands.CleanOldLogs;

public record CleanOldLogsCommand(Guid TenantId, int DaysToKeep = 90) : IRequest<int>;
