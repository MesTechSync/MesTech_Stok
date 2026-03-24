using FluentAssertions;
using MesTech.Application.Features.Logging.Commands.CleanOldLogs;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CleanOldLogsHandlerTests
{
    private readonly Mock<ILogEntryRepository> _repo;
    private readonly CleanOldLogsHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public CleanOldLogsHandlerTests()
    {
        _repo = new Mock<ILogEntryRepository>();
        _sut = new CleanOldLogsHandler(_repo.Object, NullLogger<CleanOldLogsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_DeletesOlderThanCutoff_ReturnsCount()
    {
        _repo.Setup(r => r.DeleteOlderThanAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var command = new CleanOldLogsCommand(_tenantId, 30);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().Be(42);
        _repo.Verify(r => r.DeleteOlderThanAsync(
            _tenantId,
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-29)),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_Default90Days_ComputesCorrectCutoff()
    {
        _repo.Setup(r => r.DeleteOlderThanAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new CleanOldLogsCommand(_tenantId);

        await _sut.Handle(command, CancellationToken.None);

        _repo.Verify(r => r.DeleteOlderThanAsync(
            _tenantId,
            It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-89)),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
