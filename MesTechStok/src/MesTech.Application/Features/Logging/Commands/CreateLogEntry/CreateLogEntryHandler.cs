using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Logging.Commands.CreateLogEntry;

public sealed class CreateLogEntryHandler : IRequestHandler<CreateLogEntryCommand, Guid>
{
    private readonly ILogEntryRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateLogEntryHandler> _logger;

    public CreateLogEntryHandler(
        ILogEntryRepository repo,
        IUnitOfWork uow,
        ILogger<CreateLogEntryHandler> logger)
    {
        _repo = repo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateLogEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entry = new LogEntry
        {
            TenantId = request.TenantId,
            Timestamp = DateTime.UtcNow,
            Level = request.Level,
            Category = request.Category,
            Message = request.Message,
            Data = request.Data,
            UserId = request.UserId,
            Exception = request.Exception,
            MachineName = request.MachineName ?? Environment.MachineName
        };

        await _repo.AddAsync(entry, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        LogLevel logLevel = request.Level switch
        {
            "Error" => LogLevel.Error,
            "Warning" => LogLevel.Warning,
            "Debug" => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel, "[{Category}] {Message}", request.Category, request.Message);

        return entry.Id;
    }
}
