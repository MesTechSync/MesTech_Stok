using FluentAssertions;
using MesTech.Application.Features.Logging.Commands.CreateLogEntry;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateLogEntryHandlerTests
{
    private readonly Mock<ILogEntryRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateLogEntryHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CreateLogEntryHandlerTests()
    {
        _repo = new Mock<ILogEntryRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateLogEntryHandler(_repo.Object, _uow.Object, NullLogger<CreateLogEntryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesEntryAndReturnsGuid()
    {
        var command = new CreateLogEntryCommand(
            _tenantId, "Information", "Stock", "Product updated", "data", "user1");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.Is<LogEntry>(e =>
            e.TenantId == _tenantId && e.Level == "Information" &&
            e.Category == "Stock" && e.Message == "Product updated"),
            It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullMachineName_UsesEnvironmentDefault()
    {
        var command = new CreateLogEntryCommand(_tenantId, "Debug", "System", "Test");

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.Is<LogEntry>(e =>
            e.MachineName == Environment.MachineName),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
