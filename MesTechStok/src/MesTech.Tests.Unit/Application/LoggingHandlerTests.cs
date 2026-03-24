using FluentAssertions;
using MesTech.Application.Features.Logging.Commands.CreateLogEntry;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class LoggingHandlerTests
{
    private readonly Mock<ILogEntryRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<CreateLogEntryHandler>> _logger = new();

    [Fact]
    public async Task CreateLogEntry_ValidCommand_PersistsEntry()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CreateLogEntryCommand(
            TenantId: Guid.NewGuid(), Level: "Information", Category: "Auth",
            Message: "User logged in", Data: null, UserId: "admin",
            Exception: null, MachineName: "SERVER-01");
        var handler = new CreateLogEntryHandler(_repo.Object, _uow.Object, _logger.Object);

        await handler.Handle(cmd, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.IsAny<LogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
