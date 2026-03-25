using FluentAssertions;
using MesTech.Application.Commands.CreateBarcodeScanLog;
using MesTech.Application.Queries.GetBarcodeScanLogs;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for barcode scan log handlers.
/// </summary>
[Trait("Category", "Unit")]
public class BarcodeHandlerTests
{
    private readonly Mock<IBarcodeScanLogRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // ═══════ CreateBarcodeScanLogHandler ═══════

    [Fact]
    public async Task CreateBarcodeScanLog_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new CreateBarcodeScanLogHandler(_repo.Object, _uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    // ═══════ GetBarcodeScanLogsHandler ═══════

    [Fact]
    public async Task GetBarcodeScanLogs_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetBarcodeScanLogsHandler(_repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
