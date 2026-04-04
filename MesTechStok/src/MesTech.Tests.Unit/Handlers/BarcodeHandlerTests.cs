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

    [Fact]
    public async Task CreateBarcodeScanLog_ValidCommand_ReturnsSuccessWithLogId()
    {
        var cmd = new CreateBarcodeScanLogCommand("8680000111222", "EAN13", "Scanner");
        var sut = new CreateBarcodeScanLogHandler(_repo.Object, _uow.Object);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.LogId.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.Is<MesTech.Domain.Entities.BarcodeScanLog>(
            l => l.Barcode == "8680000111222" && l.Format == "EAN13")), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════ GetBarcodeScanLogsHandler ═══════

    [Fact]
    public async Task GetBarcodeScanLogs_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetBarcodeScanLogsHandler(_repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetBarcodeScanLogs_EmptyRepo_ReturnsEmptyResult()
    {
        _repo.Setup(r => r.GetPagedAsync(1, 50, null, null, null, null, null))
            .ReturnsAsync(new List<MesTech.Domain.Entities.BarcodeScanLog>());
        _repo.Setup(r => r.GetCountAsync(null, null, null, null, null))
            .ReturnsAsync(0);

        var sut = new GetBarcodeScanLogsHandler(_repo.Object);
        var result = await sut.Handle(new GetBarcodeScanLogsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetBarcodeScanLogs_WithFilter_PassesFilterToRepo()
    {
        _repo.Setup(r => r.GetPagedAsync(1, 50, "8680", null, null, null, null))
            .ReturnsAsync(new List<MesTech.Domain.Entities.BarcodeScanLog>());
        _repo.Setup(r => r.GetCountAsync("8680", null, null, null, null))
            .ReturnsAsync(0);

        var sut = new GetBarcodeScanLogsHandler(_repo.Object);
        await sut.Handle(new GetBarcodeScanLogsQuery(BarcodeFilter: "8680"), CancellationToken.None);

        _repo.Verify(r => r.GetPagedAsync(1, 50, "8680", null, null, null, null), Times.Once);
    }
}
